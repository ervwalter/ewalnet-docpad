/**
 * Use Browserify to bundle scripts.
 */

/* Load modules */
var gulp = require('gulp');
var fs = require('fs');
var browserify = require('browserify');
var watchify = require('watchify');
var source = require("vinyl-source-stream");
var buffer = require('vinyl-buffer');
var argv = require('yargs').argv;
var reactify = require('reactify');
var uglify = require('gulp-uglify');
var del = require('del');
var notify = require('gulp-notify');
var browserSync = require('browser-sync');
var es6ify = require('es6ify');

/**
 * Watch flag used in Watchify.
 *
 * @type {boolean}
 */
var isWatch = argv.watch;

/**
 * List of libraries or modules that should be exposed outside of its bundle.
 *
 * These files will be bundled to single `common.js` file. It can be referenced
 * from other files by simply requiring the module.
 *
 * @type {Array}
 */

var vendors = [
	{require: 'fluxxor', expose: 'fluxxor'},
	{require: 'lazy.js', expose: 'lazy.js'},
	{require: 'moment', expose: 'moment'}
];

var libs = [];

var externals = [].concat(vendors, libs);

var testPath = './test/scripts',
	prodPath = '../src/files/scripts';

/**
 * Set of files to bundle.
 *
 * Each obj within this array will be bundles separately.
 *
 * @type {Array}
 */
var files = [
	{
		input: vendors,
		output: 'common.js',
		destination: [
			testPath,
			prodPath
		],
		require: true
	},
	{
		input: ['./src/games.jsx'],
		output: 'games.js',
		destination: [
			testPath,
			prodPath
		]
	},
	{
		input: ['./src/menu.jsx'],
		output: 'menu.js',
		destination: [
			testPath,
			prodPath
		]
	}
];

/**
 * Defer object to handle task ending.
 *
 * After all of the bundle is comlete, it will execute callback of gulp task
 * so that other task can wait until the task ends.
 *
 * @param {int}      max      Max number of how many call should Defer wait
 *                            until executing callback.
 * @param {Function} callback Callback of Gulp task.
 */
var Defer = function (max, callback) {
	this.max = max;
	this.count = 0;
	this.callback = callback;

	this.exec = function () {
		if (this.max === ++this.count) {
			this.callback();
		}
	};
};

/**
 * Bundle given file.
 */
var bundle = function (bundler, options) {
	startTime = new Date().getTime();

	var b = bundler.bundle()
		.on('error', notify.onError())
		.pipe(source(options.output));

	if (!isWatch) {
		b = b.pipe(buffer())
			.pipe(uglify({mangle: true}));
	}

	if (Array.isArray(options.destination)) {
		for (var i = 0; i < options.destination.length; i++) {
			b = b.pipe(gulp.dest(options.destination[i]));
		}
	} else {
		b = b.pipe(gulp.dest(options.destination));
	}

	if (isWatch) {
		b = b.pipe(browserSync.reload({stream: true}));
	}

	b = b.on('end', function () {
		time = (new Date().getTime() - startTime) / 1000;
		console.log(options.output + ' was browserified: ' + time + 's');
	});

	return b;
}

/**
 * Create bundle properties such as if its is added or required etc.
 */
var createBundleProp = function (b, options) {
	var bundler = b;

	var i = 0;
	for (i; i < options.input.length; i++) {
		if (options.require) {
			bundler.require(options.input[i].require, {
				expose: options.input[i].expose
			});
		} else {
			bundler.add(options.input[i]);

			externals.forEach(function (external) {
				bundler.external(external.expose);
			});
		}
	}
	;

	return bundler;
};

/**
 * Create single bundle using files options.
 */
var createBundle = function (options, d) {
	var bundler = browserify({
		cache: {},
		debug: isWatch,
		extensions: ['.jsx'],
		packageCache: {},
		fullPaths: false
	}).transform(reactify)
		.transform(es6ify.configure(/\.jsx?$/));

	bundler = createBundleProp(bundler, options);

	if (isWatch) {
		bundler = watchify(bundler);
		bundler.on('update', function () {
			bundle(bundler, options);
		});
	}

	return bundle(bundler, options);
};

/**
 * Create set of bundles.
 */
var createBundles = function (bundles, defer) {
	bundles.forEach(function (bundle) {
		createBundle(bundle).on('end', function () {
			defer.exec();
		});
	});
};

/**
 * Browserify task. If `--watch` option is passed, watchify will activate.
 */
gulp.task('browserify', function (done) {
	var d = new Defer(files.length, done);

	if (argv.watch) {
		isWatch = true;
	}

	createBundles(files, d);
});

gulp.task('copy-docpad-files', function () {
	return gulp.src([
		'../.out/styles/**/*.css',
		'../.out/images/**',
		'../.out/fonts/**'
	], {base: '../.out/'})
		.pipe(gulp.dest('test'));
});

gulp.task('traceur-runtime', function () {
	return gulp.src('./node_modules/traceur/bin/traceur-runtime.js')
		.pipe(uglify())
		.pipe(gulp.dest(testPath))
		.pipe(gulp.dest(prodPath));
});

gulp.task('browserSync', function () {
	browserSync({
		server: {
			baseDir: 'test/'
		}
	})
});

gulp.task('build', function () {
	gulp.start(['traceur-runtime', 'browserify']);
});

gulp.task('watch', ['copy-docpad-files'], function () {
	isWatch = true;
	gulp.start(['traceur-runtime', 'browserSync', 'browserify']);
});
