import Link from "next/link";

export default function TopNav() {
  return (
    <header className="border-b border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950">
      <nav className="mx-auto flex h-14 max-w-5xl items-center gap-6 px-6">
        <Link
          href="/"
          className="text-sm font-medium text-zinc-900 hover:text-zinc-600 dark:text-zinc-100 dark:hover:text-zinc-300"
        >
          Home
        </Link>
      </nav>
    </header>
  );
}
