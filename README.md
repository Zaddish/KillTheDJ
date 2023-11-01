# KillTheDJ
Automatically skip the Spotify DJ voice on windows.

## Overview
Are you tired of Spotify's DJ voice interrupting your music? Look no further! **KillTheDJ** automatically detects when a Spotify DJ is about to speak and skips the track for you.

## How it works
This program uses the Windows API to search for and interact with Spotify windows. It monitors the titles of these windows, which often change to reflect the currently playing track. When a track named "DJ - Up next" is detected, the program sends a "next track" media key press to skip it.

## Usage

1. Clone the repository
2. Build and run the project with `dotnet publish -r win-x64 -p:PublishSingleFile=true --self-contained false`
3. Run the binary and keep it open while spotify is open / playing music.

## Contribution
Feel free to submit pull requests or raise issues if you have suggestions for improvements or encounter any problems. This is largely untested, so I don't know how it will behave with MS store version of spotify, vs Desktop version etc.

## Disclaimer
This tool is not affiliated with or endorsed by Spotify.
