(function () {
    
    var notification_factory = function ($http, app_settings) {

        // endpoints
        var api = app_settings.api;
        var url = api + '/notifications';

        // get all notifications
        var getNotifications = function () {
            return $http.get(url)
                .then(function (response) {
                    return response.data;
                });
        };

        // search notifications
        var searchNotifications = function (search_obj) {
            return $http.post(url + '/search', search_obj)
                .then(function (response) {
                    return response.data;
                });
        };

        // get a single notification by ID
        var getByID = function (id) {
            return $http.get(url + "/" + id)
                .then(function (response) {
                    return response.data;
                });
        };

        // update a notification
        var updateNotification = function (notification) {
            return $http.put(url + "/" + id, notification)
                .then(function (response) {
                    return response.data;
                });
        };

        // mark a notificationr reviewed
        var markReviewed = function (id) {
            return $http.get(url + "/" + id + "/MarkReviewed")
                .then(function (response) {
                    return response.data;
            });
        };

        // public api
        return {
            getNotifications: getNotifications,
            getByID: getByID,
            updateNotification: updateNotification,
            searchNotifications: searchNotifications,
            markReviewed: markReviewed
        };
    };

    angular.module('NOTEY')
        .factory('notification_factory', notification_factory);
}());