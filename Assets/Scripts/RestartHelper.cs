// Scripts/RestartHelper.cs
using UnityEngine;

public static class RestartHelper
{
    // null이면 새 게임, true이면 타이머 모드, false이면 클래식 모드
    public static bool? ModeIsTimer = null;

    // 씬이 다시 시작될 때, 포인터(마우스/터치)가 "손을 뗄 때까지" 입력을 억제할지 여부
    public static bool SuppressPointerOnStart = false;
}
