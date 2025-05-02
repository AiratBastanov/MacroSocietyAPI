using MacroSocietyAPI.Encryption;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace MacroSocietyAPI.ExtensionMethod
{
    public class DecryptedIdBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.FieldName);

            if (valueProviderResult == ValueProviderResult.None)
            {
                bindingContext.ModelState.AddModelError(bindingContext.FieldName, "ID не передан");
                return Task.CompletedTask;
            }

            var encryptedId = valueProviderResult.FirstValue;

            if (!IdHelper.TryDecryptId(encryptedId, out int id, out string error))
            {
                bindingContext.ModelState.AddModelError(bindingContext.FieldName, error ?? "Неверный ID");
                return Task.CompletedTask;
            }

            bindingContext.Result = ModelBindingResult.Success(new DecryptedId(id));
            return Task.CompletedTask;
        }
    }
}
