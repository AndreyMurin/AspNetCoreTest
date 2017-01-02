(function ($) {
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
        net,

        scene,
        camera,
        renderer,
        controls,

        // приватные свойства для создания нейронов
        _geometry = new THREE.BoxGeometry(1, 1, 1),
        _material = new THREE.MeshLambertMaterial({ color: 0x00ff00 }),
        _neuronBase = new THREE.Mesh(_geometry, _material),
        createNeuron = function (x, y, z) {
            var n = _neuronBase.clone();

            // у нас все статично меняем цвета редко. а вот и нет у нас все движется (вращение камеры)
            //n.matrixAutoUpdate = false;
            // после изменения объекта обязательно вызвать .updateMatrix()
            //n.updateMatrix();

            n.position.x = x * 2;
            n.position.y = y * 2;
            n.position.z = z * 2;
            return n;
        },
        drawNet = function () {
            console.log('drawNet', netConfig);

            /*var geometry = new THREE.BoxGeometry(1, 1, 1);
            var material = new THREE.MeshBasicMaterial({ color: 0x00ff00 });
            var cube = new THREE.Mesh(geometry, material);
            scene.add(cube);
            
            camera.position.z = 5;
            var render = function () {
                requestAnimationFrame(render);

                cube.rotation.x += 0.1;
                cube.rotation.y += 0.1;

                renderer.render(scene, camera);
            };
            render();
            /**/

            //camera.position.z = 5;
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
            //stats.update();
        },
        render = function () {
            renderer.render(scene, camera);
        },
        resize = function () {
            var width = element.innerWidth();
            var height = element.innerHeight();
            camera.aspect = width / height;
            camera.updateProjectionMatrix();
            renderer.setSize(width, height);
        },
        create = function () {
            controlsCont = $(settings.controls);

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
            renderer.setClearColor(0x777777);
            renderer.setPixelRatio(window.devicePixelRatio);
            renderer.setSize(width, height);
            renderer.autoClear = true;

            controls = new THREE.OrbitControls(camera);
            controls.target.set(0, radius, 0);
            controls.update();

            element.append(renderer.domElement);

            animate();

            $(window).off('resize');
            $(window).on('resize', resize);

            return base;
        };
        base.setConfig = function (config) {
            netConfig = config;
            drawNet();
        };

        return create.call(this);
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