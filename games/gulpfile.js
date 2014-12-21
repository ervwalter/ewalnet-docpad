var gulp = require('gulp'),
	changed = require('gulp-changed'),
	browserify = require('browserify'),
	watchify = require('watchify'),
	source = require('vinyl-source-stream'),
	buffer = require('vinyl-buffer'),
	reactify = require('reactify'),
	uglify = require('gulp-uglify'),
	del = require('del'),
	notify = require('gulp-notify'),
	browserSync = require('browser-sync'),
	es6ify = require('es6ify'),
	reload = browserSync.reload,
	traceur = require('traceur'),
	p = {
		jsx: './src/games.jsx',
		bundle: 'games-react.js',
		testPath: './scripts',
		distPath: '../src/files/scripts'
	};

gulp.task('clean', function(cb) {
	del(['dist'], cb);
});

gulp.task('browserSync', function() {
	browserSync({
		server: {
			baseDir: './'
		}
	})
});

gulp.task('watchify', function() {

	var bundler = watchify(browserify(p.jsx, {
		debug: true,
		extensions: ['.jsx'],
		cache: {},
		packageCache: {},
		fullPaths: true
	}));

	function rebundle() {
		return bundler
			.bundle()
			.on('error', notify.onError())
			.pipe(source(p.bundle))
			.pipe(gulp.dest(p.testPath))
			.pipe(gulp.dest(p.distPath))
			.pipe(reload({stream: true}));
	}

	bundler.transform(reactify)
		.transform(es6ify.configure(/\.jsx?$/))
		.on('update', rebundle);

	return rebundle();
});

gulp.task('browserify', function() {
	browserify(p.jsx, {
			extensions: ['.jsx']
		}).transform(reactify)
		.transform(es6ify.configure(/\.jsx?$/))
		.bundle()
		.pipe(source(p.bundle))
		.pipe(buffer())
		.pipe(uglify())
		.pipe(gulp.dest(p.distPath));
});

gulp.task('traceur-runtime', function() {
	return gulp.src(traceur.RUNTIME_PATH)
	.pipe(uglify())
	.pipe(gulp.dest(p.testPath))
	.pipe(gulp.dest(p.distPath));
});

gulp.task('watchTask', function() {
});

gulp.task('watch', ['clean'], function() {
	gulp.start(['traceur-runtime', 'browserSync', 'watchTask', 'watchify']);
});

gulp.task('build', ['clean'], function() {
	process.env.NODE_ENV = 'production';
	gulp.start(['traceur-runtime', 'browserify']);
});

gulp.task('default', function() {
	console.log('Run "gulp watch or gulp build"');
});