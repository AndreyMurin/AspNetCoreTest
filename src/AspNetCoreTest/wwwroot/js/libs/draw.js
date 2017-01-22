( function ( $ ) {
    "use strict"; // jshint ;_;

    // задаем пространство имен
    if ( !$.bt ) $.bt = {};

    var DATA_KEY = 'bt-draw';

    $.bt.draw = function ( elem, options ) {
        var defaultSettings = {
            controls: ".js-bt-controls",
            factorX: 4, // растояние между нейронами
            factorY: 4,
            factorZ: -4,// чтобы входы были сверху
            summandX: 1, // чтобы связи не сливались с осями
            summandY: 1,
            summandZ: -1,
            summandRelsX: 2, // смещение начала связей
            summandRelsY: 2,
            summandRelsZ: -2

        },
        settings = $.extend( {}, defaultSettings, options ),
        base = this,
        element = $( elem ),
        netConfig,
        controlsCont,
        cameraInfo,
        net,

        scene,
        camera,
        renderer,
        controls,
        raycaster,
        mouse, INTERSECTED,
        firstN, secondN,

        getNeuronIndexByPosition = function ( pos ) {
            return { x: ( pos.x - settings.summandX ) / settings.factorX, y: ( pos.y - settings.summandY ) / settings.factorY, z: ( pos.z - settings.summandZ ) / settings.factorZ };
        },
        getNeuronByPosition = function ( pos ) {
            var ind = getNeuronIndexByPosition( pos );
            try {
                // test error
                //ind.x = 100;
                return net[ind.z][ind.y][ind.x];
            }
            catch ( e ) {
                return null;
            }
        },

        fillSubscribeBlock = function () {
            var args = { firstN: null, secondN: null };
            if ( firstN ) args.firstN = getNeuronIndexByPosition( firstN.position );
            if ( secondN ) args.secondN = getNeuronIndexByPosition( secondN.position );
            controlsCont.btControls( 'fillSubscribeBlock', args );
        },
        _setSelection = function ( neuron, opts, isON ) {
            neuron.material.setValues( opts );
            if ( typeof neuron.Relations !== 'undefined' ) {
                //console.log( 'setSelection firstN.Relations is set' );
                if ( isON ) {
                    neuron.Relations.visible = true;
                } else {
                    neuron.Relations.visible = false;
                }
                //neuron.Relations.updateMatrix();
            }
            if ( typeof neuron.Akson !== 'undefined' ) {
                neuron.Akson.children[0].material.setValues( opts );
            }
        },
        setSelection = function ( opts, isON ) {
            if ( typeof isON === 'undefined' ) isON = false;
            if ( !firstN ) return;
            _setSelection( firstN, opts, isON );
            if ( !secondN ) return;
            // это можно тут не устанавливать в цикле установится
            //secondN.material.setValues( opts );

            var firstP = getNeuronIndexByPosition( firstN.position );
            var secondP = getNeuronIndexByPosition( secondN.position );

            for ( var z = Math.min( firstP.z, secondP.z ), maxZ = Math.max( firstP.z, secondP.z ) ; z <= maxZ; z++ ) {
                for ( var y = Math.min( firstP.y, secondP.y ), maxY = Math.max( firstP.y, secondP.y ) ; y <= maxY ; y++ ) {
                    for ( var x = Math.min( firstP.x, secondP.x ), maxX = Math.max( firstP.x, secondP.x ) ; x <= maxX ; x++ ) {
                        _setSelection( net[z][y][x], opts, isON );
                    }
                }
            }
        },

        // отрисовка осей (и просто линий)
        buildAxis = function ( src, dst, colorHex, dashed, opacity ) {
            if ( typeof opacity === 'undefined' ) opacity = 1;
            var geom = new THREE.Geometry(),
                mat;

            if ( dashed ) {
                mat = new THREE.LineDashedMaterial( {
                    linewidth: 1, color: colorHex, dashSize: 1, gapSize: 1, opacity: opacity < 1 ? opacity : 1, transparent: true,
                } );
            } else {
                mat = new THREE.LineBasicMaterial( {
                    linewidth: 1, color: colorHex, opacity: opacity < 1 ? opacity : 1, transparent: true,
                } );
            }

            geom.vertices.push( src.clone() );
            geom.vertices.push( dst.clone() );
            geom.computeLineDistances(); // This one is SUPER important, otherwise dashed lines will appear as simple plain lines

            var axis = new THREE.Line( geom, mat, THREE.LineSegments );

            return axis;
        },
        buildAxes = function ( length ) {
            var axes = new THREE.Object3D();

            axes.add( buildAxis( new THREE.Vector3( 0, 0, 0 ), new THREE.Vector3( length, 0, 0 ), 0xFF0000, false ) ); // +X
            axes.add( buildAxis( new THREE.Vector3( 0, 0, 0 ), new THREE.Vector3( -length, 0, 0 ), 0xFF0000, true ) ); // -X
            axes.add( buildAxis( new THREE.Vector3( 0, 0, 0 ), new THREE.Vector3( 0, length, 0 ), 0x00FF00, false ) ); // +Y
            axes.add( buildAxis( new THREE.Vector3( 0, 0, 0 ), new THREE.Vector3( 0, -length, 0 ), 0x00FF00, true ) ); // -Y
            axes.add( buildAxis( new THREE.Vector3( 0, 0, 0 ), new THREE.Vector3( 0, 0, length ), 0x0000FF, false ) ); // +Z
            axes.add( buildAxis( new THREE.Vector3( 0, 0, 0 ), new THREE.Vector3( 0, 0, -length ), 0x0000FF, true ) ); // -Z

            return axes;
        },

        showCameraInfo = function () {
            return;
            //console.log('showCameraInfo');
            //fov, aspect, near, far 
            var info = camera.fov + ', ' + camera.aspect + ', ' + camera.near + ', ' + camera.far
                + '<br />' + camera.position.x + ', ' + camera.position.y + ', ' + camera.position.z;
            cameraInfo.html( JSON.stringify( camera.toJSON() ) );
        },

        normalizeMax = function ( max, value, newMax ) {
            if ( max == 0 ) return newMax; // если так получилось что максимальное знаечние весов будет ноль то вернем максимум
            var ret = value * newMax / max;
            if ( ret > newMax ) return newMax; // такое может быть когда конфиг устареет и в сети появятся новые веса или когда статистических данных нет совсем
            return ret;
        },

        // прозрачность нейрона
        getOpacityByState = function ( state ) {
            //return 1;
            // плохо ресолвить мин и макс при каждом запроосе!
            var min = -256;
            var max = 256;
            if ( netConfig.MaxState ) max = netConfig.MaxState;
            if ( netConfig.MinState ) min = netConfig.MinState;

            var nState;
            if ( state > 0 ) {
                nState = normalizeMax( max, state, 1 );
            } else {
                nState = normalizeMax( (-1 * min), -1 * state, 1 );
            }
            //console.log( 'getOpacityByState:', state, '->', nState );
            return nState;
        },

        // цвет нейрона по его состоянию        
        getColorByState = function ( state ) {
            if ( state > 0 ) return 0x00ff00;
            return 0x0000ff;
        },
        // цвет связи по ее весу
        getColorByWeight = function ( weight ) {
            if ( weight > 0 ) {
                return 0x00ff00;
            }
            return 0x0000ff; // синяя
        },
        // прозрачность связи по ее весу
        getOpacityByWeight = function ( weight ) {
            // плохо ресолвить мин и макс при каждом запроосе!
            var min = -1;
            var max = 1;
            if ( netConfig.MaxWeight ) max = netConfig.MaxWeight;
            if ( netConfig.MinWeight ) min = netConfig.MinWeight;

            var nWeight;
            if ( weight > 0 ) {
                nWeight = normalizeMax( max, weight, 1 );
            } else {
                nWeight = normalizeMax( -1 * min, -1 * weight, 1 );
            }
            //console.log( 'getOpacityByWeight:', weight, '->', nWeight );
            return nWeight;
        },
        setActivities = function ( activities ) {
            console.log( 'setActivities', activities );

        },

        longToPos = function ( i, lenX, lenY, lenZ ) {
            // вычисляем z
            var Z = Math.floor(( i / ( lenX * lenY ) ) );

            i = i - ( Z * lenX * lenY );
            var Y = Math.floor(( i / lenX ) );

            var X = i - ( Y * lenX );
            return { x: X, y: Y, z: Z };
        },
        setNeurons = function ( neurons ) {
            neurons = neurons.Neurons;
            //console.log( 'setNeurons', netConfig, neurons );
            for ( var i = 0; i < neurons.length; i++ ) {
                var neuron = neurons[i];
                var n = net[neuron.z][neuron.y][neuron.x];
                // раскраска самого нейрона
                n.material.emissive.setHex( getColorByState( neuron.Neuron.State ) );
                n.material.setValues( { opacity: getOpacityByState( neuron.Neuron.State ) } );

                if ( typeof n.Akson !== 'undefined' ) { // есть старый аксон удалим
                    scene.remove( n.Akson );
                }

                n.Akson = new THREE.Object3D();
                n.Akson.add(
                    buildAxis(
                        new THREE.Vector3( neuron.x * settings.factorX + settings.summandX, neuron.y * settings.factorY + settings.summandY, neuron.z * settings.factorZ + settings.summandZ ),
                        new THREE.Vector3( neuron.x * settings.factorX + settings.summandX + settings.summandRelsX, neuron.y * settings.factorY + settings.summandY + settings.summandRelsY, neuron.z * settings.factorZ + settings.summandZ + settings.summandRelsZ ),
                        getColorByState( neuron.Neuron.State ), false, getOpacityByState( neuron.Neuron.State )
                    )
                );
                scene.add( n.Akson );

                if ( typeof n.Relations !== 'undefined' ) { // есть старые связи их надо удалить из сцены
                    scene.remove( n.Relations );
                }
                //console.log( 'setNeurons n=', neuron.z, n );
                var rels = new THREE.Object3D(); // потом возможно поместим в переменную
                for ( var j = 0; j < neuron.Neuron.Output.length; j++ ) {

                    // отрисовка связей нейрона с вынесенем на (+1 +1 -1)
                    var pos = longToPos( neuron.Neuron.Output[j].n, netConfig.LenX, netConfig.LenY, netConfig.LenZ );
                    //console.log('rel from', neuron.x, neuron.y, neuron.z, ' to ', neuron.Neuron.Output[j].n, pos.x, pos.y, pos.z);
                    rels.add( buildAxis(
                        new THREE.Vector3( neuron.x * settings.factorX + settings.summandX + settings.summandRelsX, neuron.y * settings.factorY + settings.summandY + settings.summandRelsY, neuron.z * settings.factorZ + settings.summandZ + settings.summandRelsZ ),
                        new THREE.Vector3( pos.x * settings.factorX + settings.summandX, pos.y * settings.factorY + settings.summandY, pos.z * settings.factorZ + settings.summandZ ),
                        getColorByWeight( neuron.Neuron.Output[j].w ), false, getOpacityByWeight( neuron.Neuron.Output[j].w ) )
                    );
                }
                // прицепим связи к нейрону чтобы управлять ими (их надо периодически перерисовывать, так как будет менятся мин и макс связей и надо менять их прозрачность)
                n.Relations = rels;

                scene.add( rels );
                rels.visible = false;

            }
            // после отрисовки связей надо скинуть выделение нейронов
            setSelection( { transparent: true }, false );
            firstN = null; secondN = null;
            fillSubscribeBlock();
        },

        createNeuron = function ( x, y, z ) {
            // заранее создать низя! так как пересечения в этом случае хз как работает и выделяет сразу все объекты
            var geometry = new THREE.BoxGeometry( 1, 1, 1 );
            //var material = new THREE.MeshNormalMaterial( {
            var material = new THREE.MeshStandardMaterial( {
                color: 0xffffff,
                opacity: 0.3,
                transparent: true,
            } );
            //material.opacity = 0.5;
            var n = new THREE.Mesh( geometry, material );

            //var n = _neuronBase.clone();

            // у нас все статично меняем цвета редко. а вот и нет у нас все движется (вращение камеры)
            //n.matrixAutoUpdate = false;
            // после изменения объекта обязательно вызвать .updateMatrix()
            //n.updateMatrix();

            n.position.x = x * settings.factorX + settings.summandX;
            n.position.y = y * settings.factorY + settings.summandY;
            n.position.z = z * settings.factorZ + settings.summandZ;
            return n;
        },
        drawNet = function () {
            console.log( 'drawNet', netConfig );

            camera.position.set( 5.7033302189268404, -15.506568322186508, 4.9197803014393395 );
            //camera.updateProjectionMatrix();
            controls.update();
            net = new Array( netConfig.LenZ );
            for ( var z = 0; z < netConfig.LenZ; z++ ) {
                net[z] = new Array( netConfig.LenY );
                for ( var y = 0; y < netConfig.LenY; y++ ) {
                    net[z][y] = new Array( netConfig.LenX );
                    for ( var x = 0; x < netConfig.LenX; x++ ) {
                        var n = createNeuron( x, y, z );
                        scene.add( n );
                        net[z][y][x] = n;
                    }
                }
            }
            //renderer.render(scene, camera);
            /**/
        },
        animate = function () {
            requestAnimationFrame( animate, renderer.domElement );
            render();
            showCameraInfo();
            //stats.update();
        },
        render = function () {

            //camera.lookAt(scene.position);
            //camera.updateMatrixWorld();

            raycaster.setFromCamera( mouse, camera );
            var intersects = raycaster.intersectObjects( scene.children );
            if ( intersects.length > 0 ) {

                if ( INTERSECTED != intersects[0].object ) {
                    if ( INTERSECTED ) INTERSECTED.material.emissive.setHex( INTERSECTED.currentHex );
                    //if ( INTERSECTED ) INTERSECTED.material.update( { color: INTERSECTED.currentHex } );

                    INTERSECTED = intersects[0].object;

                    INTERSECTED.currentHex = INTERSECTED.material.emissive.getHex();
                    INTERSECTED.material.emissive.setHex( 0xff0000 );
                    //INTERSECTED.currentHex = INTERSECTED.material.color;
                    //INTERSECTED.material.update( {color: 0xff0000} );
                }
            } else {
                if ( INTERSECTED ) INTERSECTED.material.emissive.setHex( INTERSECTED.currentHex );
                //if ( INTERSECTED ) INTERSECTED.material.emissive.setHex( INTERSECTED.currentHex );

                INTERSECTED = null;
            }

            renderer.render( scene, camera );
        },
        resize = function () {
            var width = element.innerWidth();
            var height = element.innerHeight();
            camera.aspect = width / height;
            camera.updateProjectionMatrix();
            renderer.setSize( width, height );
        },

        // отработка клика на нейроне
        // первый клик выбирает начальный нейрон
        // второй клик конечный
        onClick = function () {
            if ( !INTERSECTED ) { // ничего не делаем! скидываем при двойном клике на пустом месте (скидывает после вращения камеры)
                return;
            };
            //console.log('onClick');
            setSelection( { transparent: true }, false );
            if ( firstN && secondN ) { // начали выбирать след секцию скидываем первую
                firstN = INTERSECTED;
                secondN = null;
            } else {
                if ( firstN ) {
                    secondN = INTERSECTED;
                } else {
                    firstN = INTERSECTED;
                    secondN = null;
                }
            }
            setSelection( { transparent: false }, true );
            fillSubscribeBlock();
        },
        // обрабатываем дврйной клик тока на пустом месте и скидываем выделение
        // а то при одиночном клике по пустому скидывать выделение плохо (скидывает после вращения камеры)
        onDoubleClick = function () {
            if ( !INTERSECTED ) { // клик на пустом месте скидываем выделение
                setSelection( { transparent: true }, false );
                secondN = null; firstN = null;
                fillSubscribeBlock();
                return;
            };
        },
        onDocumentMouseMove = function ( event ) {
            //event.preventDefault();
            mouse.x = ( event.clientX / window.innerWidth ) * 2 - 1;
            mouse.y = -( event.clientY / window.innerHeight ) * 2 + 1;
            //mouse.x = (event.clientX / element.innerWidth()) * 2 - 1;
            //mouse.y = -(event.clientY / element.innerHeight()) * 2 + 1;
            //console.log(mouse.x, mouse.y);
        },
        create = function () {
            controlsCont = $( settings.controls );
            cameraInfo = $( '.js-bt-camera-info' );

            var width = element.innerWidth();
            var height = element.innerHeight();
            var radius = 1;

            scene = new THREE.Scene();
            scene.add( new THREE.AmbientLight( 0xffffff, 0.3 ) );

            var light = new THREE.DirectionalLight( 0xffffff, 0.35 );
            light.position.set( 1, 1, 1 ).normalize();
            scene.add( light );


            camera = new THREE.PerspectiveCamera( 75, width / height, 0.1, 1000 );
            camera.position.set( 0.0, radius, radius * 3.5 );

            renderer = new THREE.WebGLRenderer( { antialias: true, alpha: false } );
            renderer.setClearColor( 0xffffff );//(0x777777);
            renderer.setPixelRatio( window.devicePixelRatio );
            renderer.setSize( width, height );
            renderer.autoClear = true;

            raycaster = new THREE.Raycaster();
            mouse = new THREE.Vector2();
            //document.addEventListener('mousemove', onDocumentMouseMove, false);
            //$(document).on('mousemove', onDocumentMouseMove);
            element.on( 'mousemove', onDocumentMouseMove );
            element.on( 'click', onClick );
            element.on( 'dblclick', onDoubleClick );

            controls = new THREE.OrbitControls( camera, renderer.domElement );
            controls.target.set( 0, 0, 0 );
            controls.update();

            element.append( renderer.domElement );

            scene.add( buildAxes( 1000 ) );
            animate();

            $( window ).off( 'resize' );
            $( window ).on( 'resize', resize );

            return base;
        };
        base.setConfig = function ( config ) {
            netConfig = config;
            drawNet();
        };
        base.setNeurons = function ( neurons ) {
            setNeurons( neurons );
        };
        base.setActivities = function ( activities ) {
            setActivities( activities );
        };

        return create.call( base );
    };


    // обертка для jquery
    $.fn.btDraw = function ( options, attrs ) {
        var instance;
        if ( typeof ( options ) == 'string' ) {
            var retValue = null;
            this.each( function () {
                instance = $.data( this, DATA_KEY );
                if ( instance && typeof ( instance[options] ) == 'function' ) {
                    retValue = instance[options].call( instance, attrs );
                }
                return retValue;
            } );
            return retValue;
        }
        var elements = this.each( function () {
            instance = new $.bt.draw( this, options );
            $.data( this, DATA_KEY, instance );
            return instance;
        } );
        return elements;
    };
})(jQuery);