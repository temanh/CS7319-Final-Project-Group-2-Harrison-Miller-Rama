# **RealTimeConnect**

Below is a comprehensive guide to setting up our RealTimeConnect software. It is assumed you have a Docker account and the Docker Desktop application installed on your computer along with homebrew *(if on Mac)* and your IDE *(either Visual Studio or VS Code)*. If you don't have any of the above, I would recommend setting them up before continuing.

## **Setup (Windows)**
WIP

## **Setup (Mac)**
This setup will be different if you are using a Mac.

### **Database Setup**
If you don't have virtualization software to create a Windows view *(in which just follow the setup for Windows)*, the alternative approach will be to use Visual Studio Code and setup a Docker ```mssql-server``` container for your database.

First, you will need the image for ```mssql-server```. You will be using the latest release of the 2022 version:

```
docker pull mcr.microsoft.com/mssql/server:2022-latest
```

Next, you will need to run the container. The ```SA_PASSWORD``` parameter requires a secure password with the following requirements:
- At least 8 characters in length
- Include uppercase characters
- Include lowercase characters
- Include base-10 digits
- Include non-alphanumeric symbols *(OPTIONAL)*

Otherwise, just copy/paste the following command which provides a default password satisfying the conditions:

```docker 
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=VeryStr0ngP@ssw0rd" -p 1433:1433 --name realtimeconnectdb --hostname realtimeconnectdb --platform linux/amd64 -d mcr.microsoft.com/mssql/server:2022-latest
```

Next, we will execute the environment and access the database. First, we will need ```sqlcmd``` to actually access the server. If you don't have it installed already, run the following line in your terminal *(through homebrew)*:

```brew install sqlcmd```

Now that you have  ```sqlcmd``` installed, you can now run the following line in your terminal *(of course replacing the password with your own if you altered it)*:

```sqlcmd -S localhost,1433 -U sa -P 'VeryStr0ngP@ssw0rd' -C```

Congratulations! You now are in the mssql-server. You can now type in SQL commands and make changes to the database. First, we will create our database. Run the following query in your server:

```sql
CREATE DATABASE realtimeconnectdb;
GO
```

Note that when you enter the query, the server doesn't automatically execute it. This is intentional design by Microsoft.

We will now select the database with the following query:

```sql
USE realtimeconnectdb;
GO
```

Finally, we will create all of our tables with the following queries *(Note again that you can enter in multiple queries before batch executing them with the ```GO``` command)*:

```sql
CREATE TABLE [User] (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(100) NOT NULL,
    PasswordHash NVARCHAR(200) NOT NULL
);

CREATE TABLE Contact (
    OwnerUserId INT NOT NULL,
    ContactUserId INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_Contact PRIMARY KEY (OwnerUserId, ContactUserId),

    CONSTRAINT FK_Contact_OwnerUser
        FOREIGN KEY (OwnerUserId) REFERENCES [User](Id)
        ON DELETE CASCADE,

    CONSTRAINT FK_Contact_ContactUser
        FOREIGN KEY (ContactUserId) REFERENCES [User](Id)
        ON DELETE CASCADE
);

CREATE TABLE Conversation (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL DEFAULT '',
    IsGroup BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE ConversationMember (
    UserId INT NOT NULL,
    ConversationId INT NOT NULL,

    CONSTRAINT PK_ConversationMember PRIMARY KEY (UserId, ConversationId),

    CONSTRAINT FK_CM_User
        FOREIGN KEY (UserId) REFERENCES [User](Id)
        ON DELETE CASCADE,

    CONSTRAINT FK_CM_Conversation
        FOREIGN KEY (ConversationId) REFERENCES Conversation(Id)
        ON DELETE CASCADE
);

CREATE TABLE Message (
    Id INT IDENTITY(1,1) PRIMARY KEY,

    ConversationId INT NOT NULL,
    SenderId INT NOT NULL,
    Content NVARCHAR(MAX) NOT NULL,

    SentAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT FK_Message_Conversation
        FOREIGN KEY (ConversationId) REFERENCES Conversation(Id)
        ON DELETE CASCADE,

    CONSTRAINT FK_Message_Sender
        FOREIGN KEY (SenderId) REFERENCES [User](Id)
        ON DELETE CASCADE
);

GO
```

And that is all. Your database is now setup and we can now move on to setting up Visual Studio Code.

### **IDE Setup**
Now, we will set up our environment. We need to establish the connection to the database and for our JWT service. We must edit the credentials in our ```appsettings.json``` file.

In your ```appsettings.json``` file, you will need to fill out your ```Jwt:Key``` and ```ConnectionStrings:DefaultConnection``` values with the credentials you generated earlier:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "DATABASE-CONNECTION-URL-HERE"
  },
  "Jwt": {
    "Key": "JWT-SECRET-KEY-HERE"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

If you copy/pasted the ```docker run ...``` command, you would just copy the following database connection string. Otherwise, you can manually enter your credentials:

```json
{
    "ConnectionStrings": {
        "DefaultConnection": "Server=localhost,1433;Database=realtimeconnectdb;User Id=sa;Password=VeryStr0ngP@ssw0rd;TrustServerCertificate=True;"
    },
}
```

And for the ```Jwt:Key```, we will need to generate a seperate key. Using the online tool below, create a secret that is 128 bits, copy the secret, and paste it into the ```appsettings.json``` file.

**TOOL FOR JWT SECRET GENERATION:**
https://jwtsecrets.com/#generator

We will now download the .NET SDK such that we can build our C# project. Below are the links for installing the dependencies:
- **.NET SDK**: https://dotnet.microsoft.com/en-us/download

Now, we will download the extensions to configure our environment for running. Go to **Extensions**. Install the following extensions:
- C#
- C# Dev Kit
- .NET Install Tool

You will have to enable and follow the steps when installing them. You may also have to restart Visual Studio Code to ensure the extensions take into effect.

## **Running Software**
Finally we have our environment setup. Now we will run the software. To start, change to the directory containing the ```RealTimeConnect.csproj``` file *(in the terminal in VS Code)*.

Finally, we will build and run the project by simply running the following command:

```dotnet run RealTimeConnect.csproj```

The project is now running. You can now use the url provided below and go to the login page: http://localhost:5120/login.html


## **Architecture Analysis**
### **Architecture Comparisons**
**Client-Server**:
- Centralized backend handling all logic & messaging
- Simpler (fewer moving parts)
- Tight coupling
- Single point of failure

**Microservices:**
- Split into separate services
- Loose coupling
- Allows for scalability

**Key Differences**:
- Coupling (tight vs. loose coupling)
- Scalability (scaling as a whole vs. scaling per service)
- Complexity (simplicity vs. operational overhead)


### Rationale for Selection
**Why Client-Server:**
- Lower development complexity (easier to build, test and debug)
- Faster implementation
- Reduced overhead
- Sufficient performance at scale (real-time messaging and persistence can be handled efficiently by one service)

**Why not Microservices:**
- Significant runtime and deployment overhead per service (Auth, messaging, etc.)
- Requires more infrastructure (more databases, load balancing, CI/CD, etc.)
- Benefits come at larger scale