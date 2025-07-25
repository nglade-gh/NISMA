using System.Text.Json;


namespace nbd;


public class JsonUtils<T> { 
    public T Load(string path) {
        //get the stuff from the place
        string jsonString = File.ReadAllText(path);
        //declare the thing and put the stuff from the place in the thing
        T _instance = JsonSerializer.Deserialize<T>(jsonString)!;
        return _instance;
    }

    public void Save(string path, object obj)
    {
        using (StreamWriter writer = new StreamWriter(path))
        {
            string JsonString = JsonSerializer.Serialize(obj);
            writer.WriteLine(JsonString);
        }
    }

}