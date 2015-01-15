@echo off
call gulp --cwd ./react
call docpad deploy -e static
