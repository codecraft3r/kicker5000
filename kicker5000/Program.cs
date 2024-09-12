using RconSharp;

var client = RconClient.Create(Environment.GetEnvironmentVariable("RCON_HOST"), Convert.ToInt32(Environment.GetEnvironmentVariable("RCON_PORT")));

bool connected = false;
while (!connected)
{
    try
    {
        await client.ConnectAsync();
        connected = true;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Connection failed: {ex.Message}. Retrying in 5 seconds...");
        await Task.Delay(5000);
    }
}

bool authenticated = false;
while (!authenticated)
{
    try
    {
        authenticated = await client.AuthenticateAsync(Environment.GetEnvironmentVariable("RCON_PASSWORD"));
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Authentication failed: {ex.Message}. Retrying in 5 seconds...");
        await Task.Delay(5000);
    }
}

if (authenticated) {
    Console.WriteLine("Authenticated successfully.");
    const int MIN_PLAYERS = 2;
    int checkCount = 0;
    
    while (true)
    {
        try
        {
            var players = await client.ExecuteCommandAsync("list");
            var playerCount = 0;
            if (players != null)
            {
                var match = System.Text.RegularExpressions.Regex.Match(players, @"There are (\d+) of a max of \d+ players online");
                Console.WriteLine($"Match: {match.Groups[1].Value}");
                if (match.Success)
                {
                    playerCount = int.Parse(match.Groups[1].Value);
                    Console.WriteLine($"Player count: {playerCount}");
                }
            }
            

            if (playerCount > 0 && playerCount < MIN_PLAYERS)
            {
                checkCount++;
                if (checkCount == 1) // First check, warn players
                {
                    string warningMessage = string.Format("You will be kicked in 60 seconds if there are not at least {0} players.", Environment.GetEnvironmentVariable("MIN_PLAYERS") ?? "2");
                    await client.ExecuteCommandAsync($"say {warningMessage}");
                }
                if (checkCount >= 6) // 6 checks * 10 seconds = 60 seconds
                {
                    await client.ExecuteCommandAsync("kick @a Not enough players");
                    checkCount = 0;
                }
            }
            else
            {
                checkCount = 0;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Command execution failed: {ex.Message}. Reconnecting...");
            connected = false;
            while (!connected)
            {
                try
                {
                    await client.ConnectAsync();
                    await client.AuthenticateAsync(Environment.GetEnvironmentVariable("RCON_PASSWORD"));
                    connected = true;
                }
                catch (Exception reconnectEx)
                {
                    Console.WriteLine($"Reconnection failed: {reconnectEx.Message}. Retrying in 5 seconds...");
                    await Task.Delay(5000);
                }
            }
        }

        await Task.Delay(10000); // 10 seconds
    }
}
