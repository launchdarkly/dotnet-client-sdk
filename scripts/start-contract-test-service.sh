#!/bin/bash
cd contract-tests && dotnet bin/Debug/${TESTFRAMEWORK:-netcoreapp3.1}/ContractTestService.dll
