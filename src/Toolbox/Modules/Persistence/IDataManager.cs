using Toolbox.Domain;

namespace Toolbox.Modules.Persistence;

public interface IDataManager
{
    public void Load(out Root data, out Root dataEn);

    public void Save(Root data);
    
    public void SaveEn(Root dataEn);
}