namespace Domain.ValueObjects.DummyJson;

public class DummyPosts
{
    public List<Post> posts { get; set; }
    public int total { get; set; }
    public int skip { get; set; }
    public int limit { get; set; }
    
    public class Post
    {
        public int id { get; set; }
        public string title { get; set; }
        public string body { get; set; }
        public List<string> tags { get; set; }
        public Reaction reactions { get; set; }
        public int views { get; set; }
        public int userId { get; set; }
        
        public class Reaction
        {
            public int likes { get; set; }
            public int dislikes { get; set; }
        }
    }
}