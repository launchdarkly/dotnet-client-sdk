#!/bin/bash
cd contract-tests && dotnet bin/Debug/${TESTFRAMEWORK:-net6.0}/ContractTestService.dll
