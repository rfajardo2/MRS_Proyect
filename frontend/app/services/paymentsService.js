(function () {
  'use strict';

  angular.module('mrsDrunkApp').factory('paymentsService', function ($http, apiConfig) {
    var url = apiConfig.baseUrl + '/payments';

    function normalizeCheckoutUrl(checkoutUrl) {
      if (!checkoutUrl) { return null; }
      if (/^https?:\/\//i.test(checkoutUrl)) {
        return checkoutUrl;
      }

      return apiConfig.baseUrl.replace(/\/$/, '') + checkoutUrl;
    }

    return {
      create: function (data) {
        return $http.post(url + '/create', data).then(function (res) {
          if (res.data) {
            res.data.checkoutUrlAbsolute = normalizeCheckoutUrl(res.data.checkoutUrl);
          }
          return res.data;
        });
      },
      status: function (paymentId) {
        return $http.get(url + '/' + paymentId + '/status').then(function (res) {
          if (res.data) {
            res.data.checkoutUrlAbsolute = normalizeCheckoutUrl(res.data.checkoutUrl);
          }
          return res.data;
        });
      },
      publicStatus: function (paymentId, reference) {
        return $http.get(url + '/public/' + paymentId + '/status', {
          params: { reference: reference },
          showLoader: false
        }).then(function (res) {
          if (res.data) {
            res.data.checkoutUrlAbsolute = normalizeCheckoutUrl(res.data.checkoutUrl);
          }
          return res.data;
        });
      },
      adminDashboard: function (filters) {
        return $http.get(url + '/admin', { params: filters || {} }).then(function (res) {
          return res.data;
        });
      },
      adminDetail: function (paymentId) {
        return $http.get(url + '/admin/' + paymentId).then(function (res) {
          if (res.data && res.data.payment) {
            res.data.payment.checkoutUrlAbsolute = normalizeCheckoutUrl(res.data.payment.checkoutUrl);
          }
          return res.data;
        });
      },
      executiveReport: function (filters) {
        return $http.get(url + '/admin/executive', { params: filters || {} }).then(function (res) {
          return res.data;
        });
      },
      exportExecutive: function (filters) {
        return $http.get(url + '/admin/executive/export', {
          params: filters || {},
          responseType: 'blob'
        }).then(function (res) {
          return res.data;
        });
      },
      refreshAdminStatus: function (paymentId) {
        return $http.post(url + '/admin/' + paymentId + '/refresh-status').then(function (res) { return res.data; });
      },
      reprocessApproved: function (paymentId) {
        return $http.post(url + '/admin/' + paymentId + '/reprocess-approved').then(function (res) { return res.data; });
      },
      cancelPending: function (paymentId) {
        return $http.post(url + '/admin/' + paymentId + '/cancel-pending').then(function (res) { return res.data; });
      },
      reconcileDuplicates: function (paymentId) {
        return $http.post(url + '/admin/' + paymentId + '/reconcile-duplicates').then(function (res) { return res.data; });
      },
      reconcileDuplicatesBulk: function (paymentIds) {
        return $http.post(url + '/admin/reconcile-duplicates-bulk', {
          paymentIds: paymentIds || []
        }).then(function (res) { return res.data; });
      },
      exportAdmin: function (filters) {
        return $http.get(url + '/admin/export', {
          params: filters || {},
          responseType: 'blob'
        }).then(function (res) {
          return res.data;
        });
      }
    };
  });
})();
