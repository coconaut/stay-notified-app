(function () {
    // main app - include ngSanitize so we can render some html
    angular.module('NOTEY', ['ngSanitize'])
        .constant('app_settings', { api: 'http://localhost:9000/api' });

}());