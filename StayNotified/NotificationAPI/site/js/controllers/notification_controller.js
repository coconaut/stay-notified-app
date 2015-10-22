(function () {

    var notification_controller = function ($scope, notification_factory) {
        // defaults
        $scope.status = '0';
        $scope.sort_dir = '+';
        $scope.sort_by = 'pub_date';

        var onError = function (reason) {
            $scope.error = "Error loading Notifications";
        };

        var onGetNotificationsComplete = function (data) {
            $scope.notifications = data;
        };

        var onSearchNotificationsComplete = function (data) {
            $scope.notifications = data;
        };

        var getParam = function(param, param_default) {
            if ($scope[param] != null)
            {
                return $scope[param];
            }
            else
            {
                return param_default;
            }

        };

        var getSource = function() {
            return getParam("source", "");
        };

        var getStatus = function() {
            return getParam("status", "");
        };

        var getDateStart = function () {
            return getParam("date_start", "");
        };

        var getDateEnd = function () {
            return getParam("date_end", "");
        };

        $scope.searchNotifications = function () {
            
            var NotificationSearch = {
                "source_str": getSource(),
                "status_str": getStatus(),
                "date_start_str": getDateStart(),
                "date_end_str": getDateEnd()
            };

            notification_factory.searchNotifications(NotificationSearch)
                .then(onSearchNotificationsComplete, onError);

        };

        $scope.getNotifications = function () {
            notification_factory.getNotifications()
                .then(onGetNotificationsComplete, onError);
        };

        function MarkReviewedCallbackCreator(index) {
            //return a closure to update the correct index
            return function (data) {
                if (data) {
                    // our req went through (need to verify Mongo won't return true for bad inserts...)
                    // may be better to return entire record...
                    
                    $scope.notifications[index].status.case = "Reviewed";
                }
            }
        }

        $scope.markReviewed = function (id, index) {
            // create a callback closure with our index
            var callback = MarkReviewedCallbackCreator(index);
            notification_factory.markReviewed(id)
                .then(callback, onError);
        };

    };

    angular.module('NOTEY')
        .controller('notification_controller', notification_controller);
}());