@echo off
call gulp --cwd ./react build
call docpad deploy -e static
