(function ($) {
    "use strict"; // jshint ;_;

    // задаем пространство имен
    if (!$.bt) $.bt = {};

    var DATA_KEY = 'bt-draw';

    $.bt.draw = function (elem, options) {
        var defaultSettings = {
            controls: ".js-bt-controls",
        },
        settings = $.extend({}, defaultSettings, options),
        base = this,
        element = $(elem),
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

        // отрисовка осей
        buildAxis = function (src, dst, colorHex, dashed) {
            var geom = new THREE.Geometry(),
                mat; 

            if(dashed) {
                mat = new THREE.LineDashedMaterial({ linewidth: 1, color: colorHex, dashSize: 1, gapSize: 1 });
            } else {
                mat = new THREE.LineBasicMaterial({ linewidth: 1, color: colorHex });
            }

            geom.vertices.push( src.clone() );
            geom.vertices.push( dst.clone() );
            geom.computeLineDistances(); // This one is SUPER important, otherwise dashed lines will appear as simple plain lines

            var axis = new THREE.Line(geom, mat, THREE.LineSegments);

            return axis;
        },
        buildAxes = function( length ) {
            var axes = new THREE.Object3D();

            axes.add( buildAxis( new THREE.Vector3( 0, 0, 0 ), new THREE.Vector3( length, 0, 0 ), 0xFF0000, false ) ); // +X
            axes.add( buildAxis( new THREE.Vector3( 0, 0, 0 ), new THREE.Vector3( -length, 0, 0 ), 0xFF0000, true) ); // -X
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
            cameraInfo.html(JSON.stringify( camera.toJSON()) );
        },
        // цвет нейрона по его состоянию        
        getColorByState = function (state) { },
        // цвет связи по ее весу
        getColorByWeight = function (weight) {
            if (weight > 0) {
                return 0x00ff00; // зеленая
            }
            return 0x0000ff; // синяя
        },

        setNeurons = function (neurons) {
            console.log('setNeurons', neurons);
        },

        // приватные свойства для создания нейронов
        //_geometry = new THREE.BoxGeometry(1, 1, 1),
        //_material = new THREE.MeshLambertMaterial({ color: 0x00ff00 }),
        //_neuronBase = new THREE.Mesh(_geometry, _material),
        createNeuron = function (x, y, z) {
            // заранее создать низя! так как пересечения в этом случае хз как работает и выделяет сразу все объекты
            var geometry = new THREE.BoxGeometry(1, 1, 1);
            var material = new THREE.MeshStandardMaterial({
                color: 0xffffff,
                opacity: 0.3,
                transparent: true,
            });
            //material.opacity = 0.5;
            var n = new THREE.Mesh(geometry, material);

            //var n = _neuronBase.clone();

            // у нас все статично меняем цвета редко. а вот и нет у нас все движется (вращение камеры)
            //n.matrixAutoUpdate = false;
            // после изменения объекта обязательно вызвать .updateMatrix()
            //n.updateMatrix();

            n.position.x = x * 3;
            n.position.y = y * 3;
            n.position.z = -z * 3;// чтобы входы были сверху
            return n;
        },
        getNeuronIndexByPosition = function(pos)
        {
            return {x:pos.x / 3, y:pos.y / 3,z:-pos.z / 3};
        },
        getNeuronByPosition = function (pos) {
            var ind = getNeuronIndexByPosition(pos);
            try
            {
                // test error
                //ind.x = 100;
                return net[ind.z][ind.y][ind.x];
            }
            catch (e)
            {
                return null;
            }
        },
        drawNet = function () {
            console.log('drawNet', netConfig);

            camera.position.set(5.7033302189268404, -15.506568322186508, 4.9197803014393395);
            //camera.updateProjectionMatrix();
            controls.update();
            net = new Array(netConfig.LenZ);
            for (var z = 0; z < netConfig.LenZ; z++)
            {
                net[z] = new Array(netConfig.LenY);
                for (var y = 0; y < netConfig.LenY; y++)
                {
                    net[z][y] = new Array(netConfig.LenX);
                    for (var x = 0; x < netConfig.LenX; x++)
                    {
                        var n = createNeuron(x, y, z);
                        scene.add(n);
                        net[z][y][x] = n;
                    }
                }
            }
            //renderer.render(scene, camera);
            /**/
        },
        animate = function () {
            requestAnimationFrame(animate, renderer.domElement);
            render();
            showCameraInfo();
            //stats.update();
        },
        render = function () {

            //camera.lookAt(scene.position);
            //camera.updateMatrixWorld();

            raycaster.setFromCamera(mouse, camera);
            var intersects = raycaster.intersectObjects(scene.children);
            if (intersects.length > 0) {
                //console.log('intersects -->', intersects[0].object);
                //var targetDistance = intersects[0].distance;

                //Using Cinematic camera focusAt method
                //camera.focusAt(targetDistance);

                if (INTERSECTED != intersects[0].object) {
                    if (INTERSECTED) INTERSECTED.material.emissive.setHex(INTERSECTED.currentHex);
                    INTERSECTED = intersects[0].object;
                    //console.log('object: ', getNeuronByPosition(INTERSECTED.position));
                    INTERSECTED.currentHex = INTERSECTED.material.emissive.getHex();
                    INTERSECTED.material.emissive.setHex(0xff0000);
                }
            } else {
                if (INTERSECTED) INTERSECTED.material.emissive.setHex(INTERSECTED.currentHex);
                INTERSECTED = null;
            }

            renderer.render(scene, camera);
        },
        resize = function () {
            var width = element.innerWidth();
            var height = element.innerHeight();
            camera.aspect = width / height;
            camera.updateProjectionMatrix();
            renderer.setSize(width, height);
        },

        fillSubscribeBlock = function () {
            var args = { firstN: null, secondN: null };
            if (firstN) args.firstN = getNeuronIndexByPosition(firstN.position);
            if (secondN) args.secondN = getNeuronIndexByPosition(secondN.position);
            controlsCont.btControls('fillSubscribeBlock', args);
        },
        setSelection = function (opts) {
            if (!firstN) return;
            firstN.material.setValues(opts);
            if (!secondN) return;
            secondN.material.setValues(opts);
            
            var firstP = getNeuronIndexByPosition(firstN.position);
            var secondP = getNeuronIndexByPosition(secondN.position);

            for (var z = Math.min(firstP.z, secondP.z), maxZ = Math.max(firstP.z, secondP.z) ; z <= maxZ; z++) {
                for (var y = Math.min(firstP.y, secondP.y), maxY = Math.max(firstP.y, secondP.y) ; y <= maxY ; y++) {
                    for (var x = Math.min(firstP.x, secondP.x), maxX = Math.max(firstP.x, secondP.x) ; x <= maxX ; x++) {
                        net[z][y][x].material.setValues(opts);
                    }
                }
            }
        },
        // отработка клика на нейроне
        // первый клик выбирает начальный нейрон
        // второй клик конечный
        onClick = function () {
            if (!INTERSECTED) return;
            //console.log('onClick');
            setSelection({ transparent: true });
            if (firstN && secondN) { // начали выбирать след секцию скидываем первую
                firstN = INTERSECTED;
                secondN = null;
            } else {
                if (firstN) {
                    secondN = INTERSECTED;
                } else {
                    firstN = INTERSECTED;
                    secondN = null;
                }
            }
            setSelection({ transparent: false });
            fillSubscribeBlock();
        },
        // отработка двойного клика клика на нейроне
        // двойной клик идет в куче с двумя одинарными
        /*onDoubleClick = function () {
            if (!INTERSECTED) return;
            console.log('onDoubleClick');
        },*/
        onDocumentMouseMove = function (event) {
            //event.preventDefault();
            mouse.x = (event.clientX / window.innerWidth) * 2 - 1;
            mouse.y = -(event.clientY / window.innerHeight) * 2 + 1;
            //mouse.x = (event.clientX / element.innerWidth()) * 2 - 1;
            //mouse.y = -(event.clientY / element.innerHeight()) * 2 + 1;
            //console.log(mouse.x, mouse.y);
        },
        create = function () {
            controlsCont = $(settings.controls);
            cameraInfo = $('.js-bt-camera-info');

            var width = element.innerWidth();
            var height = element.innerHeight();
            var radius = 1;

            scene = new THREE.Scene();
            scene.add(new THREE.AmbientLight(0xffffff, 0.3));

            var light = new THREE.DirectionalLight(0xffffff, 0.35);
            light.position.set(1, 1, 1).normalize();
            scene.add(light);


            camera = new THREE.PerspectiveCamera(75, width / height, 0.1, 1000);
            camera.position.set(0.0, radius, radius * 3.5);

            renderer = new THREE.WebGLRenderer({ antialias: true, alpha: false });
            renderer.setClearColor(0xffffff);//(0x777777);
            renderer.setPixelRatio(window.devicePixelRatio);
            renderer.setSize(width, height);
            renderer.autoClear = true;

            raycaster = new THREE.Raycaster();
            mouse = new THREE.Vector2();
            //document.addEventListener('mousemove', onDocumentMouseMove, false);
            //$(document).on('mousemove', onDocumentMouseMove);
            element.on('mousemove', onDocumentMouseMove);
            element.on('click', onClick);
            //element.on('dblclick', onDoubleClick);

            controls = new THREE.OrbitControls(camera, renderer.domElement);
            controls.target.set(0, 0, 0);
            controls.update();

            element.append(renderer.domElement);

            scene.add(buildAxes(1000));
            animate();

            $(window).off('resize');
            $(window).on('resize', resize);

            return base;
        };
        base.setConfig = function (config) {
            netConfig = config;
            drawNet();
        };
        base.setNeurons = function (neurons) {
            setNeurons(neurons);
        };

        return create.call(base);
    };


    // обертка для jquery
    $.fn.btDraw = function (options, attrs) {
        var instance;
        if (typeof (options) == 'string') {
            var retValue = null;
            this.each(function () {
                instance = $.data(this, DATA_KEY);
                if (instance && typeof (instance[options]) == 'function') {
                    retValue = instance[options].call(instance, attrs);
                }
                return retValue;
            });
            return retValue;
        }
        else {
            var elements = this.each(function () {
                instance = new $.bt.draw(this, options);;
                $.data(this, DATA_KEY, instance);
                return instance;
            });
            return elements;
        }
    };
})(jQuery);