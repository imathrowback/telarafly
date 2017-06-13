# Telarafly

RIFT world model viewer built in Unity3D.

## What is it?

Telarafly is a project born out of a [reddit post](https://www.reddit.com/r/Rift/comments/4gzw4g/extracting_game_model_files_and_textures/) to view the 3D models and worlds from RIFT.


## What are all these executables/DLLs in the release archive, are they bad for me?

They are needed because the program has been compiled as a 64bit executable, however certain functions (in particular database reads and decompression) requite certain decompression routine that only exist in 32bit assembly form. The 32bit executables are required to process certain information and are only needed to be run once per patch, and are are run by the progresm as needed.

##Configuration

Read the RUNNINGREADME and edit nif2obj.properties

## Running

Double click the telarafly.exe executable

## Troubleshooting

Look into the output_log.txt in the telarafly_data folder.


imathrowback

