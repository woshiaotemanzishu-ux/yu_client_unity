using Shenxiao.Generated.UI.DungeonRune;

namespace Shenxiao.Module.Core.Dungeon.Views.DungeonRune
{
    public sealed class DungeonRuneEnterItem : DungeonRuneEnterItemBind
    {
        public void SetData(DungeonModel.DunState state)
        {
            if (state == null) return;
            int floor = state.DunId >= 12000 ? state.DunId - 12000 : state.DunId;
            if (_lb_floor != null) _lb_floor.text = floor + "层  " + DungeonConfigs.GetName(state.DunId);
            if (_img_passed != null) _img_passed.gameObject.SetActive(false);
            if (_img_lock != null) _img_lock.gameObject.SetActive(false);
            if (_gp_model != null) _gp_model.gameObject.SetActive(false);
        }
    }
}
