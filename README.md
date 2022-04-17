# ChronoBot
Discord bot with multiple helpful tools. Mostly for personal use.

## Features
- Tools
  - Calculator
- Social Media
  - Supports Twitter, YouTube, and Twitch.
  - Registers users to gather latest updated contents.
  - Removes users.
  - Lists registered users in that guild.
- Games
  - Rock-Paper-Scissors

## Commands
- Tools
  - Calculator: ``!calc <calculation>``
- Social Media
  - Add user: ``<!twitteradd | !twitchadd | !youtubeadd> <(insert Twitter handle) | (insert Twitch channel name) | (insert Youtube ID/name)> [(insert channel name)] [(options, Twitter only)]``
    - Twitter options (can have multiple): 
      - Only posts: p
      - Only retweets: r
      - Only quote retweets: q
      - Only likes: l
      - Only pictures: mp
      - Only animated GIF: mg
      - Only videos: mv
      - Only any media: m
      - All of the above: no input
  - Add your following channels from Twitch: ``<!mine>``
    - This only works if your Discord name is the same as your Twitch.
  - Remove user: ``<!twitteradd | !twitchadd | !youtubeadddelete> <(insert Twitter handle) | ( insert Twitch channel name) | (insert Youtube ID/name)>``
  - Get user: ``<!twitterget | !twitchget | !youtubeget> <(insert Twitter handle) | ( insert Twitch channel name) | (insert Youtube ID/name)>``
  - List users: ``<!<twitterlist | !twitchlist | !youtubelist>``
- Games
  - Rock-Paper-Scissors 
    - Play: ``<!rps> <rock | r | paper | p | scissors | s>``
    - See statistics: ``<!rps> <statistics | stats>``
    - Resets statistics: ``<!rps> <reset>``

## Planned features
- Tools
  - Play music.
  - Reminders.
  - Selfies.
- Social Media
  - Instagram.
- Games
  - Casino slots.
  - Tic-tac-toe.
- Others
  - Slash commands.
