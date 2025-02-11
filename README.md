# Client Messenger

This is the client part of the messenger that I'm coding in my free time. If you are interested in contributing, just follow the steps below. If you have any questions, just refer to the "Rules and Other Important Infos" section.

## Steps:

1. **Fork this repository.**

2. **Open the project** in your preferred code editor and open the developer console (for Visual Studio: right-click on `ClientMessenger.csproj` -> `Open in Terminal`). If you'd rather use the Windows console search for `cmd` in your Windows search bar and open it.

3. **[Step for Windows Terminal only]** In the Windows CMD navigate to your project folder by typing `cd {path to your project}` (for example: `cd C:\Users\Praktikum\source\repos\Cristiano3120\ClientMessenger\ClientMessenger\`). Be sure to use quotes("") if your path contains whitespaces.

4. **Run the following command** to mark the Logging folder as `unchanged` in the Git index so it won’t be included in future commits. Make sure you provide the full path to the Logging folder (for example: `git update-index --assume-unchanged C:\Users\Praktikum\source\repos\Cristiano3120\ClientMessenger\ClientMessenger\Logging\`), and make sure to use quotes("") if the path contains whitespaces. Now you need to **run the second command** which does the same thing just for a different file. This time it's the configuration file **located** at `Settings/Settings.json`. To mark it as `unchanged` type for example: `git update-index --assume-unchanged C:\Users\Praktikum\source\repos\Cristiano3120\ClientMessenger\ClientMessenger\Settings\Settings.json`.
   
5. Now **read** the **rules** and **important infos** below.
   
6. Go to the [Server Messenger repository](https://github.com/Cristiano3120/ServerMessenger) now and follow the README there.

## Rules and Other Important Infos:

- **First**, if you have **ANY** questions, just hit me up on any of my linked socials (check my GitHub profile) or add me on Discord `Cristiano26`.

- **Second**, **ANY** contribution is appreciated. If you **add** a `comment`, **fix** a `bug`, **provide** `coding recommendations`, or whatever, I will be grateful for any help that I get.

### Rules:

- Please use the **editor config** file available in the repository. If you dislike the config, just ask me first and we can figure it out.

- Please **follow** the naming conventions and style guidelines:

```cs
public class ShowCasingClass
{
    // For private fields (use _ prefix)
    private readonly int _thisIsAField;

    // Public property (auto-implemented)
    public int ThisIsAProperty { get; set; }

    // Public field (only do this when you want to use readonly otherwise use a property)
    public readonly string ThisIsAReadonlyField;

    // Constants (Use constants to avoid magic numbers)
    public const byte ThisIsAConst = 3;

    // You can also use SHOUTING_SNAKE_CASE for things like Windows api values like here:
    private const int WM_SYSCOMMAND = 0x112;

    // Example for a magic number which should be avoided (in this case it is avoided with a param but you could also do this with a const):
    static Logger()
    {
        AllocConsole();
        _pathToLogFile = MaintainLoggingSystem(maxAmmountLoggingFiles: 5); // here I used a param to avoid the magic number
    }

    private static string MaintainLoggingSystem(int maxAmountLoggingFiles)
    {
        string pathToLoggingDic = Client.GetDynamicPath(@"Logging/");
        string[] files = Directory.GetFiles(pathToLoggingDic, "*.md");

        if (files.Length >= maxAmountLoggingFiles)
        {
            files = [.. files.OrderBy(File.GetCreationTime)];
            // +1 to make room for a new File
            int filesToRemove = files.Length - maxAmmountLoggingFiles + 1;

            for (int i = 0; i < filesToRemove; i++)
            {
                File.Delete(files[i]);
            }
        }

        var timestamp = DateTime.Now.ToString("dd-MM-yyyy/HH-mm-ss");
        var pathToNewFile = Client.GetDynamicPath($"Logging/{timestamp}.md");
        File.Create(pathToNewFile).Close();
        return pathToNewFile;
    }

    // For methods
    public void ThisIsAMethod<T>(int param)
    {
        // For local variables (ONLY USE 'var' WHEN THE TYPE IS APPARENT!)
        var thisIsALocalVar = "";

        // Wrong(because what the method returns is not apparent):
        var thisIsAReturnVar = DoSomething();

        // Right:
        byte[] thisIsAReturnVar2 = DoSomething();
        var streamWriter = new StreamWriter("");
        var image = Image.FromFile("");
        var byteArr = ConvertToByteArr();
    }

    //Always use the prefix async if the method is asynchronous
    public async Task GetInfosFromTheDatabaseAsnyc()
    {
        await DoSomethingAsync();
    }

    //This is a example for a database request. Always make the query constant and work with ASYNC
    public static async Task RemoveUserAsync(string email)
    {
        const string query = "DELETE FROM users WHERE email = @email";
        var npgsqlConnection = new NpgsqlConnection(_connectionString);
        await npgsqlConnection.OpenAsync();

        var cmd = new NpgsqlCommand(query, npgsqlConnection);
        cmd.Parameters.AddWithValue("@email", Security.EncryptAesDatabase<string, string>(email));
        await cmd.ExecuteNonQueryAsync();
    }
}
