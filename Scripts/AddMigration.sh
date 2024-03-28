#!/usr/bin/env bash

if [ -z "$1" ]
  then
    echo "No argument supplied"
    exit 1
fi

dotnet ef migrations add $@ --project src/Fumo.Database --output-dir Migrations
