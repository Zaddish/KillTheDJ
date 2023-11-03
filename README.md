# KillTheDJ
KillTheDJ listens for changes in the window title of the Spotify process to determine the current state of music playback (playing, paused, resumed) and can perform actions like skipping tracks when certain conditions are met.

## Features
- Can detect when Spotify plays a new track, resumes a track, or pauses.
- Has the capability to skip tracks automatically based on specified conditions.
- Writes the current playing track to a text file for external use.

## Dependencies
- .NET Framework 6
- Access to Spotify __desktop application__

## Setup
1. Clone this repository to your local machine.
2. Open the project in Visual Studio.
3. Ensure .NET Framework is properly installed and configured.
4. Build the solution to restore any NuGet packages if necessary and compile the code.
5. Run the application - make sure Spotify is open before running this program.

## Usage
Once the application is running, it will automatically start monitoring Spotify's playback state if Spotify is open. No user interaction is required.

By default, when the title contains "DJ - Up next", the application will skip the track.

The current playback status is logged to the console and written to a file named `playing.txt` in the application's running directory.

## Limitations
- This application is only compatible with Windows due to its reliance on the user32.dll for API calls.
- It is designed to work with the Spotify desktop application, not the web player or mobile app.

## Contribution
Feel free to submit pull requests or raise issues if you have suggestions for improvements or encounter any problems. This is largely untested, so I don't know how it will behave with MS store version of spotify, vs Desktop version etc.

## Disclaimer
This project is not affiliated with Spotify. It was created for educational purposes and personal use only. Please use responsibly.
