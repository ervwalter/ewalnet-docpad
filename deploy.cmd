@echo off
call gulp --cwd ./games build
call docpad deploy -e static
