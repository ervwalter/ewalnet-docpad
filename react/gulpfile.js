var gulp = require('gulp');
var gutil = require('gulp-util');
var runSequence = require('run-sequence');
var notify = require('gulp-notify');
var browserify = require('browserify');
var to5ify = require('6to5ify');
var uglifyify = require('./uglifyify'); // customized version of the standard filter
var path = require('path');
var browserSync = require('browser-sync');
var decomponentify = require('decomponentify');
var fs = require('fs');

var files = [
	'games.jsx',
	'menu.jsx'
];

var debug = false;

var Defer = function (max, callback) {
	this.max = max;
	this.count = 0;
	this.callback = callback;

	this.exec = function () {
		this.count++;
		if (this.max === this.count) {
			this.callback();
		}
	};
};

function writeFile(filename, defer) {
	var stream = fs.createWriteStream(filename);
	if (defer.exec) {
		stream.on('finish', function () {
			gutil.log('Wrote', gutil.colors.magenta(path.basename(filename)));
			defer.exec();
		});
	}
	return stream;
}

gulp.task('build', function (done) {
	// since multiple files are being created, this semaphore is used to call the done() callback only after all of the files have been written
	var defer = new Defer(files.length + 1, function (err) {
		if (debug) {
			browserSync.reload();
		}
		done(err);
	});

	var entries = files.map(function (file) {
		return './' + path.join('src/', file);
	});

	var outputs = files.map(function (file) {
		return writeFile('./' + path.join('build/scripts/', path.basename(file, path.extname(file))) + '.js', defer);
	});

	var options = {
		debug: debug,
		extensions: ['.jsx']
	};

	var bundler = browserify(options)
		.transform(decomponentify)
		.transform(to5ify.configure({optional: ['coreAliasing']}));

	if (!debug) {
		bundler.transform({global: true}, uglifyify);
	}

	bundler.plugin('factor-bundle', {outputs: outputs})
		.exclude('crypto');

	for (var i = 0; i < entries.length; i++) {
		bundler.add(entries[i]);
	}

	bundler.bundle()
		.on('error', notify.onError())
		.pipe(writeFile('./build/scripts/common.js', defer));
});

gulp.task('copy', ['build'], function () {
	return gulp.src('build/scripts/*', {base: 'build/'})
		.pipe(gulp.dest('../src/files/'));
});

gulp.task('browser-sync', ['build', 'copy'], function () {
	return browserSync({
		server: {
			baseDir: ['build', '../.out/']
		}
	})
});

gulp.task('watch', function () {
	debug = true;
	runSequence('browser-sync', function () {
		gulp.watch('src/**/*', ['build', 'copy']);
	});
});

gulp.task('default', ['build', 'copy']);

