(function() {
  var NUMBER_STRING, R_ISO8601_STR, app, htmlEncode, int, isDate, isNumber, isString, processGames, processPlays, updateGameProperties, username;

  username = 'edwalter';

  app = angular.module('GamesApp', ['ngResource', 'ngTouch', 'ngSanitize', 'ui.bootstrap']);

  app.directive('playsTooltip', function($http, $compile) {
    return {
      restrict: 'A',
      scope: {
        game: '=playsTooltip'
      },
      link: function(scope, element, attrs) {
        var compiledContent, template;
        template = '<div><p ng-repeat="play in game.plays"><i>Played {{ play.playDate | relativeDate}}</i> - {{ play.comments }}</p></div>';
        compiledContent = $compile(template)(scope);
        console.log(compiledContent);
        return $(element).qtip({
          content: {
            title: scope.game.name,
            text: compiledContent
          },
          position: {
            my: 'bottom center',
            at: 'top center',
            target: $(element),
            viewport: $(window)
          },
          hide: {
            fixed: true
          },
          style: {
            classes: 'qtip-bootstrap qtip-play'
          }
        });
      }
    };
  });

  app.directive('tooltipHtml', function($http, $compile, $templateCache) {
    return {
      restrict: 'A',
      scope: {
        content: '=tooltipHtml',
        title: '@tooltipTitle'
      },
      link: function(scope, element, attrs) {
        var compiledContent, template;
        template = '<div ng-bind-html="content"></div>';
        compiledContent = $compile(template)(scope);
        return $(element).qtip({
          content: {
            title: scope.title,
            text: compiledContent
          },
          position: {
            my: 'left center',
            at: 'right center',
            target: $(element),
            viewport: $(window)
          },
          hide: {
            fixed: true
          },
          style: {
            classes: 'qtip-bootstrap qtip-play'
          }
        });
      }
    };
  });

  app.controller('GamesCtrl', function($scope, $resource, $location, $http, $filter) {
    var collectionApi, playsApi;
    playsApi = $resource("http://www.ewal.net/api/plays", {}, {
      get: {
        isArray: true,
        transformResponse: $http.defaults.transformResponse.concat(function(data) {
          return processPlays(data);
        })
      }
    });
    collectionApi = $resource("http://www.ewal.net/api/collection", {}, {
      get: {
        isArray: true,
        transformResponse: $http.defaults.transformResponse.concat(function(data) {
          return processGames(data);
        })
      }
    });
    $scope.plays = playsApi.get();
    $scope.games = collectionApi.get();
    $scope.playsLoaded = function() {
      var _ref;
      return ((_ref = $scope.plays) != null ? _ref.length : void 0) > 0;
    };
    $scope.gamesLoaded = function() {
      var _ref;
      return ((_ref = $scope.games) != null ? _ref.length : void 0) > 0;
    };
    $scope.range = function(n, max) {
      var num, _i, _results;
      n = Math.min(n, max);
      _results = [];
      for (num = _i = 1; 1 <= n ? _i <= n : _i >= n; num = 1 <= n ? ++_i : --_i) {
        _results.push(num);
      }
      return _results;
    };
    $scope.gameCount = function(games) {
      var count, game, _i, _len;
      count = 0;
      for (_i = 0, _len = games.length; _i < _len; _i++) {
        game = games[_i];
        if (game.owned && (!game.isExpansion)) {
          count++;
        }
      }
      return count;
    };
    $scope.playDetails = function(game) {
      var details, play;
      details = (function() {
        var _i, _len, _ref, _results;
        _ref = game.plays;
        _results = [];
        for (_i = 0, _len = _ref.length; _i < _len; _i++) {
          play = _ref[_i];
          _results.push("<i>Played " + ($filter('relativeDate')(play.playDate)) + "</i> - " + (htmlEncode(play.comments)));
        }
        return _results;
      })();
      return details.join('<br/><br/>');
    };
    $scope.sortByName = function() {
      return $location.search('sort', 'name');
    };
    $scope.sortByRating = function() {
      return $location.search('sort', 'rating');
    };
    $scope.sortByPlays = function() {
      return $location.search('sort', 'plays');
    };
    return $scope.$watch(function() {
      return $location.search().sort;
    }, function(sort) {
      switch (sort) {
        case 'rating':
          return $scope.sortBy = ['-rating', '+sortableName'];
        case 'plays':
          return $scope.sortBy = ['-numPlays', '+sortableName'];
        default:
          return $scope.sortBy = '+sortableName';
      }
    });
  });

  updateGameProperties = function(game) {
    game.name = game.name.trim().replace(/\ \ +/, ' ');
    if (game.name.toLowerCase().endsWith('- base set')) {
      game.name = game.name.substr(0, game.name.length - 10).trim();
    }
    if (game.name.toLowerCase().endsWith('– base set')) {
      game.name = game.name.substr(0, game.name.length - 10).trim();
    }
    game.sortableName = game.name.toLowerCase().trim().replace(/^the\ |a\ |an\ /, '');
  };

  processPlays = function(plays) {
    var cutoff, game, result, _i, _len;
    result = _.chain(plays).groupBy('gameId').sortBy(function(game) {
      return game[0].playDate;
    }).reverse().value();
    cutoff = result[Math.min(10, result.length)][0].playDate;
    result = _.map(result, function(item) {
      var game;
      game = {
        gameId: item[0].gameId,
        image: item[0].image,
        name: item[0].name,
        thumbnail: item[0].thumbnail,
        plays: _.chain(item).filter(function(play) {
          return play.playDate > cutoff;
        }).map(function(play) {
          return {
            playDate: play.playDate,
            comments: play.comments
          };
        }).value()
      };
      return game;
    });
    for (_i = 0, _len = result.length; _i < _len; _i++) {
      game = result[_i];
      updateGameProperties(game);
    }
    return result;
  };

  processGames = function(games) {
    var game, _i, _len;
    for (_i = 0, _len = games.length; _i < _len; _i++) {
      game = games[_i];
      updateGameProperties(game);
    }
    return games;
  };

  app.filter('floor', function() {
    return function(input) {
      return Math.floor(parseFloat(input)).toString();
    };
  });

  htmlEncode = function(value) {
    return $('<div/>').text(value).html();
  };

  R_ISO8601_STR = /^(\d{4})-?(\d\d)-?(\d\d)(?:T(\d\d)(?::?(\d\d)(?::?(\d\d)(?:\.(\d+))?)?)?(Z|([+-])(\d\d):?(\d\d))?)?$/;

  NUMBER_STRING = /^\-?\d+$/;

  isString = function(value) {
    return typeof value === 'string';
  };

  isNumber = function(value) {
    return typeof value === 'number';
  };

  isDate = function(value) {
    return value instanceof Date;
  };

  int = function(str) {
    return parseInt(str, 10);
  };

  app.filter('count', function() {
    return function(arr) {
      return arr.length;
    };
  });

  app.filter('relativeDate', function($filter) {
    var dateFilter, jsonStringToDate;
    jsonStringToDate = function(string) {
      var date, dateSetter, h, m, match, ms, s, timeSetter, tzHour, tzMin;
      if (match = string.match(R_ISO8601_STR)) {
        date = new Date(0);
        tzHour = 0;
        tzMin = 0;
        dateSetter = (match[8] ? date.setUTCFullYear : date.setFullYear);
        timeSetter = (match[8] ? date.setUTCHours : date.setHours);
        if (match[9]) {
          tzHour = int(match[9] + match[10]);
          tzMin = int(match[9] + match[11]);
        }
        dateSetter.call(date, int(match[1]), int(match[2]) - 1, int(match[3]));
        h = int(match[4] || 0) - tzHour;
        m = int(match[5] || 0) - tzMin;
        s = int(match[6] || 0);
        ms = Math.round(parseFloat("0." + (match[7] || 0)) * 1000);
        timeSetter.call(date, h, m, s, ms);
        return date;
      }
      return string;
    };
    dateFilter = $filter('date');
    return function(date, format) {
      var diff, m, sod;
      if (isString(date)) {
        if (NUMBER_STRING.test(date)) {
          date = int(date);
        } else {
          date = jsonStringToDate(date);
        }
      }
      if (isNumber(date)) {
        date = new Date(date);
      }
      if (!isDate(date)) {
        return date;
      }
      m = moment(date);
      sod = moment().startOf('day');
      diff = m.diff(sod, 'days', true);
      if (diff < -6) {
        return dateFilter(date, format);
      } else if (diff < -1) {
        return "" + (m.format('dddd'));
      } else if (diff < 0) {
        return 'Yesterday';
      } else if (diff === 0) {
        return 'Today';
      } else {
        return dateFilter(date, format);
      }
    };
  });

  app.directive('trackLoaded', function($animate) {
    return {
      link: function(scope, element, attrs) {
        return element.bind("load", function(event) {
          return element.addClass('loaded');
        });
      }
    };
  });

}).call(this);
