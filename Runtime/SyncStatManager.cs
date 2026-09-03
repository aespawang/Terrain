using System.Collections.Generic;

namespace GaiaTerrain
{
    /// <summary>
    /// 负责统一调用所有 ISyncStat 对象的 SyncStat()
    /// </summary>
    public class SyncStatManager
    {
        private readonly List<ISyncStat> _targets = new();
        
        public void Register(ISyncStat target)
        {
            if (target != null && !_targets.Contains(target))
                _targets.Add(target);
        }
        
        public void SyncStatAll()
        {
            foreach (var t in _targets)
                t.SyncStat();
        }
        
        public void Clear() => _targets.Clear();
    }
}