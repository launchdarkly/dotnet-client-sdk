#!/usr/bin/env bash

# used only for CI build

if [ -f "~/project/xamarin.android-oss_v9.2.99.172_Linux-x86_64_master_d33bbd8e-Release" ]; then
	echo "Xamarin Android cache exists"
else
	wget https://jenkins.mono-project.com/view/Xamarin.Android/job/xamarin-android-linux/lastSuccessfulBuild/Azure/processDownloadRequest/xamarin-android/xamarin.android-oss_v9.2.99.172_Linux-x86_64_master_d33bbd8e-Release.tar.bz2
	tar xjf ./xamarin.android-oss_v9.2.99.172_Linux-x86_64_master_d33bbd8e-Release.tar.bz2
	echo "Downloaded Xamarin Android from Mono Jenkins"
fi
