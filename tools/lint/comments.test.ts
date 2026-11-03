// The comment lint (Redline brief 064; see comments.ts): its failing examples, and the repository holding none.
import { describe, expect, test } from 'bun:test';
import { lint, scan } from './comments';

const kinds = (file: string, text: string) => scan(file, text).map(h => `${h.kind}@${h.line}`);

describe('comment lint: failing examples', () => {
  test('a statement swallowed into a trailing comment fails (C# and TS)', () => {
    expect(kinds('a.cs', 'var a = 1; // the cache key cm.Kind = ps.Kind;')).toEqual(['swallowed@1']);
    expect(kinds('a.cs', 'Run(); // the probe, once a frame Icon(fireIcon, 3);')).toEqual(['swallowed@1']);
    expect(kinds('a.cs', 'x.Go(); // gate follow.style.left = 4;')).toEqual(['swallowed@1']);
    expect(kinds('a.cs', 'Run(); // then return value;')).toEqual(['swallowed@1']);
    expect(kinds('a.cs', 'Run(); // a loop for (var i = 0; i < n; i++) {')).toEqual(['swallowed@1']);
    expect(kinds('a.ts', "expect(a).toBe(1); // the robot swaps its mesh expect(b.scale[0]).toBe(1);")).toEqual(['swallowed@1']);
  });
  test('a whole-line comment that is one statement fails', () => {
    expect(kinds('a.cs', '    // beatsBox.style.flexDirection = FlexDirection.Row;')).toEqual(['statement@1']);
    expect(kinds('a.cs', '    // Icon(fireIcon, 3);')).toEqual(['statement@1']);
    expect(kinds('a.ts', '// const x = compute(y);')).toEqual(['statement@1']);
    expect(kinds('a.ts', '// return q;')).toEqual(['statement@1']);
  });
  test('an unclosed block comment fails, at the line it opens', () => {
    expect(kinds('a.cs', 'int a;\n/* the rest of the file\nint b;\n')).toEqual(['unclosed@2']);
  });
});

describe('comment lint: prose and legitimate code pass', () => {
  test('prose with brackets, semicolons and statements inside does not fail', () => {
    expect(kinds('a.cs', 'Run(); // (rivals, police); the hold\'s window (0.64 -> 0.75 in view);')).toEqual([]);
    expect(kinds('a.cs', 'Run(); // the call Foo(bar); is read once, then the next frame.')).toEqual([]);
    expect(kinds('a.cs', '// guard included (for the clock only);')).toEqual([]);
    expect(kinds('a.ts', '//   gap     seconds to add after what it waits for (default 0);')).toEqual([]);
    expect(kinds('a.cs', '/// <summary>x = y; is documented here;</summary>')).toEqual([]);
  });
  test('a // inside a string, a verbatim string, a char, a regex or a block comment is not a comment', () => {
    expect(kinds('a.cs', 'var u = "http://x/y"; var v = @"a//b""c//d";')).toEqual([]);
    expect(kinds('a.ts', "const re = /^(stop|bank)\\//.test(e) || ok(a);")).toEqual([]);
    expect(kinds('a.ts', "const s = `a // b ${x}`; const t = 'c // d';")).toEqual([]);
    expect(kinds('a.cs', 'int a; /* note x = 1; */ int b;')).toEqual([]);
    expect(kinds('a.cs', '/* a\n x = 1; // not code\n*/\nint b;')).toEqual([]);
  });
});

describe('the repository', () => {
  test('no C# or TS line has code swallowed into a comment, a commented-out statement or an unclosed /*', () => {
    const r = lint();
    expect(r.files).toBeGreaterThan(500);
    expect(r.hits.map(h => `${h.file}:${h.line} ${h.kind}: ${h.text.slice(0, 160)}`)).toEqual([]);
  });
});
