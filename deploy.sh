#!/bin/sh

gulp --cwd ./games build
docpad deploy -e static

