using UnityEngine;

namespace ProjectAI.Core.Attributes
{
    /// <summary>
    /// 인스펙터에서 수정할 수 없도록(Read-Only) 필드를 회색으로 비활성화하는 어트리뷰트입니다.
    /// </summary>
    public class ReadOnlyAttribute : PropertyAttribute { }
}
