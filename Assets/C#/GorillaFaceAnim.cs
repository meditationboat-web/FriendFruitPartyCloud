using UnityEngine;

public class GorillaFaceAnim : MonoBehaviour
{
  private Animator anim;

  void Start()
  {
    anim = GetComponent<Animator>();
  }

  public void PlayAnim(string triggerName)
  {
    if (anim != null) anim.SetTrigger(triggerName);
  }

  /// <summary>
  /// 【新增】直接强制切换到指定动画状态（通过 CrossFade，不用配置 Trigger）
  /// </summary>
  public void PlayState(string stateName)
  {
    if (anim != null) anim.CrossFade(stateName, 0.1f);
  }

  public bool IsPlayingState(string stateName = "OpenMouth")
  {
    if (anim == null) anim = GetComponent<Animator>();
    if (anim == null) return false;

    AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
    return stateInfo.IsName(stateName);
  }
}
