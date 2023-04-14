using System;
using System.Linq;
using System.Collections.Generic;

using System.Reflection;
using ObisoftNet.Http;

namespace Models
{
    public class ChunkData 
    {         
        public bool canceled {get;set;}
        public string[] chunks {get;set;}
        public string cookie {get;set;}
        public string filename {get;set;}
        public double filesize {get;set;}
        public int chunksize {get;set;}
        public string host {get;set;}
        public double index {get;set;}
        public string password {get;set;}
        public string sid {get;set;}
        public string state {get;set;}
        public string username {get;set;}

        public HttpSession session { get; set; }
        
        public override string ToString(){
        	string result = "";
        	object thisobj = (object)this;
        	var type = this.GetType();
        	var fields = type.GetFields();
        	foreach(var f in fields){
        		var name = f.Name;
        		var value = f.GetValue(thisobj);
        		string val = "None";
        		if (val!=null)
        		val = value.ToString();
        		result += $"{name}: {val}";
        	}
        	return result;
        }
       
   
    }
    
  
}