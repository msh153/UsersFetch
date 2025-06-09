using System.Net.Http.Json;
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public Address Address { get; set; }
}

public class Address
{
    public string City { get; set; }
}

public class Post
{
    public int UserId { get; set; }
    public string Title { get; set; }
}

public class Program
{
    public static async Task Main(string[] args)
    {
        using var httpClient = new HttpClient();

        var users = await httpClient.GetFromJsonAsync<List<User>>("https://jsonplaceholder.typicode.com/users");
        var posts = await httpClient.GetFromJsonAsync<List<Post>>("https://jsonplaceholder.typicode.com/posts");

        if (users == null || posts == null)
        {
            Console.WriteLine("Error fetching data.");
            return;
        }

        var userPostCounts = posts
            .GroupBy(p => p.UserId)
            .ToDictionary(g => g.Key, g => g.Count());

        var filteredUsers = users
            .Where(u => u.Address?.City?.StartsWith("S", StringComparison.OrdinalIgnoreCase) == true)
            .ToList();

        Console.WriteLine($"Users:");
        foreach (var user in filteredUsers)
        {
            int postCount = userPostCounts.ContainsKey(user.Id) ? userPostCounts[user.Id] : 0;

            Console.WriteLine($"\tName: {user.Name}");
            Console.WriteLine($"\tCity: {user.Address.City}");
            Console.WriteLine($"\tPosts count: {postCount}");
            Console.WriteLine("\t----------------------");
        }

        filteredUsers = users
            .Where(u => u.Id > 5)
            .ToList();

        Console.WriteLine($"Posts from users with ID more than 5:");
        foreach (var user in filteredUsers)
        {
            var userPosts = posts.Where(p => p.UserId == user.Id).ToList();
            if (userPosts.Any())
            {
                Console.WriteLine($"\tPosts from {user.Name}:");
                foreach (var post in userPosts)
                {
                    Console.WriteLine($"\t- {post.Title}");
                }
            }
            else
            {
                Console.WriteLine("\t- No posts available.");
            }

            Console.WriteLine("\t----------------------");
        }

        filteredUsers = users
        .Where(u => u.Name.StartsWith("L", StringComparison.OrdinalIgnoreCase) &&
                     userPostCounts.ContainsKey(u.Id) && userPostCounts[u.Id] > 5)
        .ToList();

        Console.WriteLine($"Users with names starting with 'L' and more than 5 posts:");
        foreach (var user in filteredUsers)
        {
            int postCount = userPostCounts[user.Id];

            Console.WriteLine($"\tName: {user.Name}");
            Console.WriteLine($"\tPosts count: {postCount}");
            Console.WriteLine("\t----------------------");
        }
    }
}
