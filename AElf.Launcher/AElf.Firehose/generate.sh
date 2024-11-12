#!/bin/bash

protoc --csharp_out=Generated --proto_path=Proto sf/aelf/type/v1/core.proto sf/aelf/type/v1/kernel.proto
