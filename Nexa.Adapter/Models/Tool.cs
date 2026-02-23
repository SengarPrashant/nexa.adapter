namespace Nexa.Adapter.Models
{
    public class Tool
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Object InputSchema { get; set; }


        public static object BuildTool(Tool tool) => new
        {
            toolSpec = new
            {
                name = tool.Name,
                description = tool.Description,
                inputSchema = new { json = tool.InputSchema }
            }
        };
}
    
}