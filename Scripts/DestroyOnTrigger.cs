using UnityEngine;

public class DestroyOnTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // 检查触发器的游戏对象是否是 usv
        if (other.gameObject.CompareTag("Ship")) // 确保 usv 游戏对象有一个标签叫 "USV"
        {
            Destroy(gameObject); // 销毁 cube
        }
    }
}