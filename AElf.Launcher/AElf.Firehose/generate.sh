#!/bin/bash

protoc --csharp_out=Generated --proto_path=Proto aelf/core.proto aelf/kernel.proto
