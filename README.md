First time Setup

	1. Setup Extension Settings
	2. Update configSetting in appsettings.json file
		"DirectScaleSecret": "" from extension settings
		"ExtensionSecrets": "" from extension settings
		"Client": "" name of client https://<client>.corpadmin.directscalestage.com

For Local Debugging

	1. use ngrok for hooks or event handlers local debugging https://ngrok.com/
	2. create account on agrok using mail
	3. install ngrok 
	4. configure ngrok only first time (you got token while create account)
			command : ngrok authtoken <token>
			Authtoken saved to configuration file: C:\Users\pc44/.ngrok2/ngrok.yml
	5. run you web extension project locally
	6. run this command on ngrok to get server path 
			command : ngrok http https://localhost:<port> -host-header="localhost:<port>"
	7. you get server path of you local project e.g. https://<random-code>.ngrok.io/
	8. need to update hooks or event path on corpadmin development
			Hook Manager : https://<client>.corpadmin.directscalestage.com/Corporate/Admin/Extension/Hooks
			Event Manager : https://<client>.corpadmin.directscalestage.com/Corporate/Admin/Extension/Events
	9. Update path for hook/event https://prnt.sc/0VEKdZSJiMfd with the new server you get in step 7.
	10. After done test make sure to reset all path in which you change in step 9
