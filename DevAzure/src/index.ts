import { createCli } from "./presentation/cli";

async function main(): Promise<void> {
  try {
    await createCli().parseAsync(process.argv);
  } catch (error) {
    const message = error instanceof Error ? error.message : String(error);
    console.error(`devazure: ${message}`);
    process.exitCode = 1;
  }
}

void main();
