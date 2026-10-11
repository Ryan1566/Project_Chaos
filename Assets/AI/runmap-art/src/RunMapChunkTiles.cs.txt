using UnityEngine;
using UnityEngine.Tilemaps;

namespace RunMap
{
    /// <summary>
    /// 挂在每个房间块 prefab 根上，直接给出"这块用哪两张瓦片"。
    ///
    /// **为什么需要它**：运行时装配（Pass E）要往房间里补瓦片（封堵未用开口、加地形起伏），
    /// 但运行时不能像编辑器那样 `AssetDatabase.LoadAssetAtPath` 去拿瓦片资产。
    /// 两条路：① 把瓦片也塞进 Resources 再按路径加载（等于把同一批资产拷两份）；
    /// ② 让**房间块自己带上**它的瓦片引用 —— prefab 本来就引用着这些 Tile，顺手暴露出来即可。
    /// 这里走 ②：不重复资产、不猜路径、不会和美术换皮脱节。
    /// </summary>
    public class RunMapChunkTiles : MonoBehaviour
    {
        [Tooltip("实心瓦片（补墙、填底、柱子用）")]
        public Tile fill;

        [Tooltip("顶面瓦片（补出来的台面用，玩家要能站在上面）")]
        public Tile top;
    }
}
