import csv
import ctypes
import io
import subprocess
import threading
import tkinter as tk
from tkinter import messagebox
from ctypes import wintypes


class MEMORYSTATUSEX(ctypes.Structure):
    _fields_ = [
        ("dwLength", wintypes.DWORD),
        ("dwMemoryLoad", wintypes.DWORD),
        ("ullTotalPhys", ctypes.c_ulonglong),
        ("ullAvailPhys", ctypes.c_ulonglong),
        ("ullTotalPageFile", ctypes.c_ulonglong),
        ("ullAvailPageFile", ctypes.c_ulonglong),
        ("ullTotalVirtual", ctypes.c_ulonglong),
        ("ullAvailVirtual", ctypes.c_ulonglong),
        ("ullExtendedVirtual", ctypes.c_ulonglong),
    ]


class FILETIME(ctypes.Structure):
    _fields_ = [("dwLowDateTime", wintypes.DWORD), ("dwHighDateTime", wintypes.DWORD)]


kernel32 = ctypes.windll.kernel32
user32 = ctypes.windll.user32


def filetime_value(value):
    return (value.dwHighDateTime << 32) | value.dwLowDateTime


def system_stats():
    memory = MEMORYSTATUSEX()
    memory.dwLength = ctypes.sizeof(memory)
    kernel32.GlobalMemoryStatusEx(ctypes.byref(memory))

    idle = FILETIME()
    kernel = FILETIME()
    user = FILETIME()
    if not kernel32.GetSystemTimes(ctypes.byref(idle), ctypes.byref(kernel), ctypes.byref(user)):
        cpu = 0.0
    else:
        total = filetime_value(kernel) + filetime_value(user)
        cpu = 0.0 if total == 0 else (1.0 - filetime_value(idle) / total) * 100.0

    return cpu, memory.dwMemoryLoad


def gpu_usage(pid):
    """Read Task Manager's GPU Engine counter without opening or modifying Roblox."""
    if not pid:
        return None
    try:
        result = subprocess.run(
            ["typeperf", r"\GPU Engine(*)\Utilization Percentage", "-sc", "1", "-y"],
            capture_output=True,
            text=True,
            timeout=3,
            creationflags=subprocess.CREATE_NO_WINDOW,
        )
        rows = list(csv.reader(io.StringIO(result.stdout)))
        if len(rows) < 2:
            return None
        total = 0.0
        found = False
        marker = f"pid_{pid}_"
        for header, value in zip(rows[0], rows[-1]):
            if marker.lower() not in header.lower():
                continue
            try:
                total += float(value.replace(",", "."))
                found = True
            except ValueError:
                pass
        return min(total, 100.0) if found else None
    except (OSError, subprocess.SubprocessError):
        return None


def tasklist_process():
    """Slow, Task Manager-style process lookup used by safe mode."""
    for name in ("RobloxPlayerBeta.exe", "RobloxStudioBeta.exe"):
        try:
            output = subprocess.check_output(
                ["tasklist", "/FI", f"IMAGENAME eq {name}", "/FO", "CSV", "/NH"],
                text=True,
                stderr=subprocess.DEVNULL,
                creationflags=subprocess.CREATE_NO_WINDOW,
            )
            for row in csv.reader(io.StringIO(output)):
                if len(row) >= 2 and row[0].lower() == name.lower():
                    return int(row[1]), name
        except (OSError, subprocess.SubprocessError, ValueError):
            pass
    return None, None


def fast_process_lookup():
    """Use the same read-only task listing with a shorter refresh interval."""
    return tasklist_process()


def make_click_through(window):
    try:
        hwnd = window.winfo_id()
        style = user32.GetWindowLongW(hwnd, -20)
        user32.SetWindowLongW(hwnd, -20, style | 0x00000020 | 0x00000080)
    except Exception:
        pass


def start_global_f3(root):
    """Register F3 globally so it works while Roblox has focus."""
    def listen():
        if not user32.RegisterHotKey(None, 1, 0, 0x72):
            return
        message = wintypes.MSG()
        while user32.GetMessageW(ctypes.byref(message), None, 0, 0) > 0:
            if message.message == 0x0312:
                root.after(0, root.event_generate, "<F3>")
        user32.UnregisterHotKey(None, 1)

    threading.Thread(target=listen, daemon=True).start()


def choose_monitoring_mode(root):
    result = {"safe": None}
    dialog = tk.Toplevel(root)
    dialog.title("Microstrap Overlay warning")
    dialog.configure(bg="#171725")
    dialog.attributes("-topmost", True)
    dialog.resizable(False, False)
    dialog.grab_set()

    body = (
        "Microstrap Overlay needs to identify the Roblox process to show GPU usage.\n\n"
        "Hyperion may check tools that search for the Roblox process. This overlay does not inject code, open a Roblox process handle, or modify Roblox, but monitoring tools can still be flagged.\n\n"
        "Use safe mode? Safe mode uses the slower Windows task-list lookup.\n"
        "We are not responsible for bans or other consequences from using this feature."
    )
    tk.Label(
        dialog, text=body, bg="#171725", fg="#f0edff", justify="left", wraplength=520,
        padx=22, pady=20, font=("Segoe UI", 10),
    ).pack()
    buttons = tk.Frame(dialog, bg="#171725")
    buttons.pack(fill="x", padx=22, pady=(0, 18))

    def safe_mode():
        result["safe"] = True
        dialog.destroy()

    def continue_mode():
        first = messagebox.askyesno(
            "Are you sure?",
            "Continue without safe mode? This uses a more frequent process lookup and may be more noticeable to Hyperion.",
            parent=dialog,
        )
        if not first:
            return
        second = messagebox.askyesno(
            "Are you really sure?",
            "The overlay is not required to play Roblox. Continue monitoring anyway?",
            parent=dialog,
        )
        if second:
            result["safe"] = False
            dialog.destroy()

    tk.Button(buttons, text="Use safe mode", command=safe_mode, width=18).pack(side="left")
    tk.Button(buttons, text="No, continue", command=continue_mode, width=18).pack(side="right")
    dialog.protocol("WM_DELETE_WINDOW", safe_mode)
    dialog.update_idletasks()
    dialog.geometry(f"560x{dialog.winfo_reqheight()}+{(dialog.winfo_screenwidth() - 560) // 2}+{(dialog.winfo_screenheight() - dialog.winfo_reqheight()) // 2}")
    root.wait_window(dialog)
    return result["safe"]


class Overlay:
    def __init__(self):
        self.root = tk.Tk()
        self.root.title("Microstrap Overlay")
        self.root.configure(bg="#101019")
        self.root.attributes("-topmost", True)
        self.root.attributes("-alpha", 0.88)
        self.root.overrideredirect(True)
        self.root.geometry("220x58+0+0")
        self.root.bind("<F3>", self.toggle_panel)
        self.root.bind("<Escape>", lambda _: self.hide_panel())
        self.root.after(100, self.position)
        self.root.after(250, make_click_through, self.root)
        start_global_f3(self.root)

        self.hud = tk.Frame(self.root, bg="#101019", padx=10, pady=7)
        self.hud.pack(fill="both", expand=True)
        self.hud_text = tk.Label(
            self.hud,
            text="Microstrap Overlay\nCPU --%   GPU --%   FPS --",
            bg="#101019",
            fg="#f0edff",
            justify="left",
            font=("Segoe UI", 9),
        )
        self.hud_text.pack(anchor="w")

        self.panel = tk.Toplevel(self.root)
        self.panel.title("Microstrap Overlay")
        self.panel.configure(bg="#171725")
        self.panel.attributes("-topmost", True)
        self.panel.attributes("-alpha", 0.96)
        self.panel.geometry("330x430")
        self.panel.protocol("WM_DELETE_WINDOW", self.hide_panel)
        self.panel.withdraw()
        self.panel_text = tk.Label(
            self.panel,
            text="",
            bg="#171725",
            fg="#f0edff",
            justify="left",
            anchor="nw",
            padx=18,
            pady=18,
            font=("Segoe UI", 10),
        )
        self.panel_text.pack(fill="both", expand=True)
        self.safe_mode = choose_monitoring_mode(self.root)
        self.process_lookup = tasklist_process if self.safe_mode else fast_process_lookup
        self.refresh_delay = 2000 if self.safe_mode else 500
        self.running = True
        self.refresh()

    def position(self):
        width = self.root.winfo_screenwidth()
        self.root.geometry(f"220x58+{width - 240}+24")

    def toggle_panel(self, _event=None):
        if self.panel.state() == "withdrawn":
            self.panel.deiconify()
            self.panel.geometry(f"330x430+{self.root.winfo_screenwidth() - 350}+92")
        else:
            self.hide_panel()

    def hide_panel(self):
        self.panel.withdraw()

    def refresh(self):
        if not self.running:
            return
        cpu, memory = system_stats()
        pid, name = self.process_lookup()
        gpu = gpu_usage(pid)
        gpu_text = "--" if gpu is None else f"{gpu:.0f}%"
        process_text = f"{name} (PID {pid})" if pid else "Roblox process not found"
        mode_text = "Safe mode" if self.safe_mode else "Frequent lookup"
        self.hud_text.configure(text=f"Microstrap Overlay\nCPU {cpu:.0f}%   GPU {gpu_text}   FPS --")
        self.panel_text.configure(
            text=(
                "MICROSTRAP OVERLAY  ·  F3 to close\n\n"
                f"Roblox\n  {process_text}\n\n"
                f"CPU usage\n  {cpu:.1f}%\n\n"
                f"Memory usage\n  {memory}%\n\n"
                f"Roblox GPU engine usage\n  {gpu_text}\n\n"
                "FPS\n  Not available through safe monitoring\n\n"
                f"Monitoring mode\n  {mode_text}\n"
                "  Windows performance counters\n"
                "  No code injection or process modification"
            )
        )
        self.root.after(self.refresh_delay, self.refresh)

    def run(self):
        try:
            self.root.mainloop()
        finally:
            self.running = False


if __name__ == "__main__":
    Overlay().run()
