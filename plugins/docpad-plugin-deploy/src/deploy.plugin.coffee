# Prepare
safeps = require('safeps')
pathUtil = require('path')
safefs = require('safefs')
{TaskGroup} = require('taskgroup')

# Export
module.exports = (BasePlugin) ->
	# Define
	class DeployPlugin extends BasePlugin
		# Name
		name: 'deploy'

		# Config
		config:
			deployRemote: 'azure'
			deployBranch: 'master'
			environment: 'static'

		# Do the Deploy
		deployWithGit: (next) =>
			# Prepare
			docpad = @docpad
			config = @getConfig()
			{outPath,rootPath} = docpad.getConfig()
			opts = {}

			# Log
			docpad.log 'info', 'Deployment starting...'

			# Tasks
			tasks = new TaskGroup().once('complete', next)

			# Check paths
			tasks.addTask (complete) ->
				# Check
				if outPath is rootPath
					err = new Error("Your outPath configuration has been customised. Please remove the customisation in order to use the deployment plugin")
					return next(err)

				# Apply
				opts.outGitPath = pathUtil.join(outPath, '.git')

				# Complete
				return complete()

			# Check environment
			tasks.addTask (complete) ->
				# Check
				if config.environment not in docpad.getEnvironments()
					err = new Error("Please run again using: docpad deploy --env #{config.environment}")
					return next(err)

				# Complete
				return complete()

			# Generate the static environment to out
			tasks.addTask (complete) ->
				docpad.log 'debug', 'Performing static generation...'
				docpad.action('generate', complete)

			# Fetch the last log so we can add a meaningful commit message
			tasks.addTask (complete) ->
				docpad.log 'debug', 'Fetching log messages...'
				safeps.spawnCommand 'git', ['log', '--oneline'], {cwd:rootPath}, (err,stdout,stderr) ->
					# Error?
					return complete(err)  if err

					# Extract
					opts.lastCommit = stdout.split('\n')[0]

					# Complete
					return complete()

			# Initialize a git repo inside the out directory and push it to the deploy branch
			tasks.addTask (complete) ->
				docpad.log 'debug', 'Performing push...'
				gitCommands = [
					['add', '--all']  # make sure we add absoutely everything in the out directory, even files that could be ignored by our global ignore file (like bower_components)
					['commit', '-m', 'some stuff [automated commit and deployment]']
					['push', config.deployRemote, config.deployBranch]
				]
				safeps.spawnCommands 'git', gitCommands, {cwd:outPath, stdio:'inherit'}, (err) ->
					# Error?
					return complete(err)  if err

					# Log
					docpad.log('info', 'Deployment to GitHub Pages completed successfully')

					# Complete
					return complete()

			# Start the deployment
			tasks.run()

			# Chain
			@


		# =============================
		# Events

		# Console Setup
		consoleSetup: (opts) =>
			# Prepare
			docpad = @docpad
			config = @getConfig()
			{consoleInterface,commander} = opts

			# Deploy command
			commander
			.command('deploy')
			.description("Deploys your #{config.environment} website to the #{config.deployRemote}/#{config.deployBranch} branch")
			.action consoleInterface.wrapAction(@deployWithGit)

			# Chain
			@