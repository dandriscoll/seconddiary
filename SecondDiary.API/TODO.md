Please make the following changes, one line at a time:
* Parse and validate auth tokens and use them to get the user ID
* Add UI for viewing and setting system prompts. The UI should allow line-by-line edits.
* Add Azure Communication Service libraries to send emails. Include setup in README.md
* Add a "<userid>-emailsettings" entity for saving an email address and time of the day when the recommendation email should be sent
* Set up a background task to send the email when the specified time arrives every day. The timer can be up to 5 minutes away from the specified time

Remove each line when the task is done