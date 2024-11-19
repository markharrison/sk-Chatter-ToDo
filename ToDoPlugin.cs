using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using OllamaSharp.Models.Chat;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chatter
{
    public class ToDoPlugin
    {
        private List<ToDoItem> _toDoItems = new List<ToDoItem>();

        private IConfiguration _config;
        bool _traceOn = false;

        public ToDoPlugin(IConfiguration config) 
        {
            _config = config;
            _traceOn = bool.TryParse(config["TraceOn"], out bool traceOn) && traceOn;
        }


        [KernelFunction("Get_ToDo_Items")]
        [Description("Get all To-Do items")]
        [return: Description("List of all ToDo items")]
        public List<ToDoItem> GetToDoItems()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Get all To-Dos");
            Console.ForegroundColor = ConsoleColor.White;
            return _toDoItems;
        }

        [KernelFunction("Add_ToDo_Item")]
        [Description("Add a To-Do item - specify the 'description'")]
        public string AddToDoItem(string description)
        {
            string _description = description.ToUpper();
            _toDoItems.Add(new ToDoItem { Description = _description });
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Add To-Do : {_description}  ");
            Console.ForegroundColor = ConsoleColor.White;
            return $"Item Added {_description}";
        }

        [KernelFunction("Remove_ToDo_Item")]
        [Description("Remove a To-Do item that matches the 'description'")]
        public string RemoveToDoItem(string description)
        {
            string _description = description.ToUpper();
            var itemToRemove = _toDoItems.FirstOrDefault(item => item.Description == _description);
            if (itemToRemove == null)
            {
                return $"Item not found {_description}";
            }

            _toDoItems.Remove(itemToRemove);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Remove To-Do : {_description}  ");
            Console.ForegroundColor = ConsoleColor.White;

            return $"Item removed {_description}";
        }
    }

    public class ToDoItem
    {
        required public string Description { get; set; }
    }

}
