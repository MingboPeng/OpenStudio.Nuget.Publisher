# OpenStudio.Nuget.Publisher
This repo prepares OpenStudio NuGet package and publish them to NuGet. 

# Steps:
    - Get the latest version of OpenStudio build action with CSharpSDK: https://github.com/NREL/OpenStudio/actions/ 
    - Get the Run ID of the GitHub Action run
    - Update release.yaml file with the new run id and OpenStudio version