#!/usr/bin/env node

import fs from 'fs'
import yargs from 'yargs'
import { hideBin } from 'yargs/helpers'
import { S3, GetObjectCommand } from '@aws-sdk/client-s3'

const argv = yargs(hideBin(process.argv))
    .command('s3 <key>', 'write contents of key', (yargs) => {
        yargs.positional('key', {
            describe: 'Key for bucket to fetch',
            type: 'string'
        })
    })
    .option('region',          { type:'string', default:'us-east-1' })
    .option('accountId',       { type:'string', default:'b95f38ca3a6ac31ea582cd624e6eb385' })
    .option('accessKeyId',     { type:'string', default:process.env.R2_ACCESS_KEY_ID })
    .option('secretAccessKey', { type:'string', default:process.env.R2_SECRET_ACCESS_KEY })
    .option('bucket',          { type:'string', default:'diffusion' })
    .demandCommand()
    .argv
//console.log(argv)

const s3 = new S3({
    credentials: {
        accessKeyId: argv.accessKeyId,
        secretAccessKey: argv.secretAccessKey,
    },
    endpoint: `https://${argv.accountId}.r2.cloudflarestorage.com`,
    region: argv.region
})

const run = async () => {
    let Key = argv._[0]
    if (Key[0] === '/') Key = Key.substring(1)
    const fileName = Key.substring(Key.lastIndexOf('/') + 1)
    const r = await s3.send(new GetObjectCommand({
        Bucket: argv.bucket,
        Key,
    }))
    r.Body.pipe(fs.createWriteStream(fileName, { flags: 'w' }))
}
run()
