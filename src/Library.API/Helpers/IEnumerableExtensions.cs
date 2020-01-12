using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Library.API.Helpers
{
    // Now for the heavy lifting. ShapeData will return a IEnumerable of type
    // ExpandoObjects. ExpandoObjects let us dynamically create objects by adding
    // key/value pairs: the name of the property and it's value. So essentially 
    // ExpandoObjects contain a dictionary.
    public static class IEnumerableExtensions
    {
        public static IEnumerable<ExpandoObject> ShapeData<TSource>(
            this IEnumerable<TSource> source,
            string fields)
        {
            // Check if the list, of AuthorDto in our case, is null
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            // Create a list to hold our ExpandoObjects. The list will
            // essentially be our AuthorDto with only fields requested.
            var expandoObjectList = new List<ExpandoObject>();

            // Create a list with PropertyInfo objects on TSource. This list
            // will contain all of the fieds/properties that we want in our
            // list of ExpandoObjects
            var propertyInfoList = new List<PropertyInfo>();

            if (string.IsNullOrWhiteSpace(fields))
            {
                // Since fields is null, we want all the properties/fields in our
                // return object, so it'll looks just like AuthorDto.
                var propertyInfos = typeof(TSource)
                        .GetProperties(BindingFlags.Public | BindingFlags.Instance);

                // Add all the properties, we'll use this later in the last foreach
                // loop.
                propertyInfoList.AddRange(propertyInfos);
            }
            else
            {
                // Ok, our fields is not empty so we have to go through all
                // the fields and and find the matching property in our
                // Type TSource, in our case AuthorDto.

                // The field are separated by ",", so we split it.
                var fieldsAfterSplit = fields.Split(',');

                // Let's iterate through our fields and find the matching property.
                foreach (var field in fieldsAfterSplit)
                {
                    // Trim each field, as it might contain leading 
                    // or trailing spaces. Can't trim the var in foreach,
                    // so use another var.
                    var propertyName = field.Trim();

                    // Use reflection to the the type of TSource, again in our case
                    // that will be something like AuthorDto. Then we'll GetProperty
                    // with the field name, we trimmed and got propertyName above.
                    var propertyInfo = typeof(TSource)
                        .GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                    // If it doesn't exist then this broke. We should get this if we
                    // use our TypeHasProperties in TypeHelperService. Check the controller.
                    if (propertyInfo == null)
                    {
                        throw new Exception($"Property {propertyName} wasn't found on {typeof(TSource)}");
                    }

                    // Let's add the property to our list of properties we will
                    // return.
                    propertyInfoList.Add(propertyInfo);
                }
            }

            // We have the list of properties we want included in the response. Either we are returning
            // everything or we filtered. Either way we have a list of properties.

            // Iterate through each list item, in our case AuthorDto. Then we'll iterate again
            // to only include the properties in our properties list.
            foreach (TSource sourceObject in source)
            {
                // Create an ExpandoObject that will hold the 
                // selected properties & values
                var dataShapedObject = new ExpandoObject();

                // Use our propertyInfoList that contains the fields we want to return.
                foreach (var propertyInfo in propertyInfoList)
                {
                    // GetValue returns the value of the property on the source object
                    // For example, propertyInfor in this iteration could be Name and
                    // we want it's value for the sourceObject/AuthorDto.
                    var propertyValue = propertyInfo.GetValue(sourceObject);

                    // Add our property name and it's value to our ExpandoObject.
                    ((IDictionary<string, object>)dataShapedObject).Add(propertyInfo.Name, propertyValue);
                }

                // Done with our first sourceObject/AuthorDto. We'll iterate again if there
                // are more.
                expandoObjectList.Add(dataShapedObject);
            }

            // Return the list
            return expandoObjectList;
        }
    }
}
