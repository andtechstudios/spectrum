.DEFAULT_GOAL := deploy

deploy:
	butler push unity/builds/html andtechstudios/spectrum:html

serve:
	live-server unity/builds/html
