using Microsoft.EnterpriseManagement.Mom.Modules.DataItems;

using System.Collections.Generic;
using System.Reflection;

namespace Maximus.Connectivity.SQLExtension.Modules
{
  public class PropertyBagObject
  {
    public PropertyBagDataItem GetPropertyBagDataItem()
    {
      Dictionary<string, object> bagItem = new Dictionary<string, object>();
      foreach (PropertyInfo myProperty in GetType().GetProperties())
      {
        bagItem.Add(myProperty.Name, myProperty.GetValue(this));
      }
      Dictionary<string, Dictionary<string, object>> collections = new Dictionary<string, Dictionary<string, object>> { { "", bagItem } };
      return new PropertyBagDataItem(null, collections);
    }
  }
}
