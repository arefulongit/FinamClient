#!/usr/bin/env python3
"""Project entry point for the TAUSIK bootstrap.

The TAUSIK library lives in the ``.tausik-lib`` git submodule, so the
documented command from AGENTS.md (``python bootstrap/bootstrap.py ...``)
must exist in the project root. This wrapper forwards argv to the library
script.

One project policy is applied here: ``--ide all`` deploys only the hosts this
project actually uses (``PROJECT_IDES``). TAUSIK's ``verify`` preparation step
calls ``bootstrap.py --ide all`` on every run; without this narrowing that
step would recreate six ~10 MB profiles (cursor, codex, kilo, opencode, ...)
that nobody in this repository runs, and every verify would leave the working
tree dirty with generated files. ``bootstrap_drift`` compares only deployed
profiles, so the narrowed set stays green.

Keep the submodule initialized::

    git submodule update --init .tausik-lib
"""

from __future__ import annotations

import os
import runpy
import sys

PROJECT_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
LIB_BOOTSTRAP_DIR = os.path.join(PROJECT_DIR, ".tausik-lib", "bootstrap")
TARGET = os.path.join(LIB_BOOTSTRAP_DIR, "bootstrap.py")

#: Hosts this project deploys. Claude Code reads ``.claude/`` and ``CLAUDE.md``;
#: GigaCode (Qwen Code) reads ``.qwen/`` and ``QWEN.md``. Add a host here only
#: when someone actually runs it against this repository.
PROJECT_IDES = ("claude", "qwen")


def _run(argv: list[str]) -> int:
    """Execute the library bootstrap with ``argv`` in-process; return its exit code."""
    saved_argv = sys.argv
    sys.argv = [TARGET, *argv]
    try:
        runpy.run_path(TARGET, run_name="__main__")
    except SystemExit as exc:  # the library calls sys.exit() on failure
        return int(exc.code or 0)
    finally:
        sys.argv = saved_argv
    return 0


def _replace_ide(argv: list[str], ide: str) -> list[str]:
    """Return ``argv`` with the value of ``--ide`` replaced by ``ide``."""
    out = list(argv)
    for index, token in enumerate(out):
        if token == "--ide" and index + 1 < len(out):
            out[index + 1] = ide
            return out
        if token.startswith("--ide="):
            out[index] = f"--ide={ide}"
            return out
    return out


def main() -> None:
    if not os.path.isfile(TARGET):
        sys.exit(
            "bootstrap: TAUSIK library not found at .tausik-lib/bootstrap/bootstrap.py. "
            "Run: git submodule update --init .tausik-lib"
        )

    sys.path.insert(0, LIB_BOOTSTRAP_DIR)
    argv = sys.argv[1:]

    # ``--ide all`` is TAUSIK's default for the verify preparation step. Deploy
    # the project's own hosts instead of every supported profile.
    wants_all = any(
        token == "--ide=all"
        or (token == "--ide" and index + 1 < len(argv) and argv[index + 1] == "all")
        for index, token in enumerate(argv)
    )
    if not wants_all:
        sys.exit(_run(argv))

    for ide in PROJECT_IDES:
        code = _run(_replace_ide(argv, ide))
        if code:
            sys.exit(code)
    sys.exit(0)


if __name__ == "__main__":
    main()
