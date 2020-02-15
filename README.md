# AzureCLI-Extractor
AzureCLI-Extractor abuses the insecure storage of AzureCLI access and refresh tokens, bypassing Multi-Factor Authentication to create a new global administrator.


# AzureCLI
AzureCLI is a command-line tool which allows system administrators to easily manage Azure Sources. Additionally, this tool is designed to be easy to learn but powerful enough to build custom automation using Azure Resources.
Before using any of the features of the tool, the users needs to sign in with the az login command. 
This type authentication supports Multi-Factor Authentication and is the most popular one.
When a user tries to login, Azure-CLI opens an Azure sign-in page which will allow you to sign in, and after a successful login, it will send the Access Token Information on a local web server it started.

![](/images/path.png)

As can bee seen from the image below the tokens are stored in clear-text. 

![](/images/token.png)

Technically, this is not a vulnerability as that's how it's supposed to work.
The behaviour can be improved by saving the access and refresh tokens only when, the users specifies so.

In case an attacker manages to extract the content of the accessToken.json file, by compromising a backup server, using a file read primitive etc. This tool can be used to create a new Global Admin account.
Since it uses the Graph API to create the user, it will bypass Multi-Factor authentication.


# Usage

AzureCLI-Extractor supports the following commands :

* adduser - Adds a Global Admin User.

  * -d, --displayname         Required. User display name.

  * -u, --username            Required. Account username.

  * -a, --accountprincipal    Required. The account principal name. It should be something like
                            user@company.onmicrosoft.com / user@company.com .

  * -p, --password            Required. Account password.

  * --help                    Display this help screen.

  *  --version                 Display version information.
  
* gettoken - Retrieve an updated user access token.

# Example
Retrieve an updated token.
![](/images/gettoken.png)

Add a new global admin user.
![](/images/adduser.png)
