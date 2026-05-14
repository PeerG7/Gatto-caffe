// =====================================================================
// IGamepadNavigable — interface สำหรับ canvas ที่รองรับ gamepad
//
// ใส่ไฟล์นี้ไว้ใน Scripts/Interfaces/ หรือ Scripts/Input/
//
// canvas ไหนที่ต้องการรับ input จาก gamepad/keyboard ให้ implement
// interface นี้ แล้วเรียก:
//   PlayerInteract2D.RegisterActiveCanvas(this)    → ตอน OpenCanvas()
//   PlayerInteract2D.UnregisterActiveCanvas(this)  → ตอน CloseCanvas()
//
// OnConfirm()  → เมื่อกด Interact / A / E ขณะ canvas เปิดอยู่
// OnBack()     → เมื่อกด B / Escape
// OnNavigate() → D-pad / Left Stick เพื่อเลื่อน focus ระหว่างปุ่ม
// =====================================================================
public interface IGamepadNavigable
{
    void OnConfirm();
    void OnBack();
    void OnNavigate(UnityEngine.Vector2 direction);
}