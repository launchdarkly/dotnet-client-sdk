#!/bin/bash

# used only for CI build

# Origin from https://github.com/travis-ci/travis-cookbooks/blob/master/community-cookbooks/android-sdk/files/default/android-wait-for-emulator

set +e

bootanim=""
failcounter=0
timeout_in_sec=360

echo -n "Waiting for emulator to start"

until [[ "$bootanim" =~ "stopped" ]]; do
  bootanim=`adb shell getprop init.svc.bootanim 2>&1 &`
  if [[ "$bootanim" =~ "device not found" || "$bootanim" =~ "device offline"
    || "$bootanim" =~ "running" ]]; then
    let "failcounter += 1"
    echo -n "."
    if [[ $failcounter -gt $timeout_in_sec ]]; then
      echo "Timeout ($timeout_in_sec seconds) reached; failed to start emulator"
      exit 1
    fi
  fi
  sleep 2
done

echo " ready"
