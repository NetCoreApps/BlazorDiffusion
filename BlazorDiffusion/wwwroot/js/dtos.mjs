/* Options:
Date: 2023-01-01 03:54:13
Version: 6.51
Tip: To override a DTO option, remove "//" prefix before updating
BaseUrl: https://localhost:5001

//AddServiceStackTypes: True
//AddDescriptionAsComments: True
//IncludeTypes: 
//ExcludeTypes: 
//DefaultImports: 
*/

"use strict";
export class AuditBase {
    constructor(init) { Object.assign(this, init); }
    createdDate;
    createdBy;
    modifiedDate;
    modifiedBy;
    deletedDate;
    deletedBy;
}
export class Artifact extends AuditBase {
    constructor(init) { super(init); Object.assign(this, init); }
    id;
    creativeId;
    fileName;
    filePath;
    contentType;
    contentLength;
    width;
    height;
    seed;
    prompt;
    nsfw;
    averageHash;
    perceptualHash;
    differenceHash;
    background;
    lqip;
    quality;
    likesCount;
    albumsCount;
    downloadsCount;
    searchCount;
    temporalScore;
    score;
    rank;
    refId;
}
export class AlbumArtifact {
    constructor(init) { Object.assign(this, init); }
    id;
    albumId;
    artifactId;
    description;
    createdDate;
    modifiedDate;
    artifact;
}
export class Album extends AuditBase {
    constructor(init) { super(init); Object.assign(this, init); }
    id;
    name;
    description;
    slug;
    tags;
    refId;
    ownerId;
    ownerRef;
    primaryArtifactId;
    private;
    rating;
    likesCount;
    downloadsCount;
    searchCount;
    score;
    rank;
    prefColumns;
    artifacts;
}
export class AlbumLike {
    constructor(init) { Object.assign(this, init); }
    id;
    albumId;
    appUserId;
    createdDate;
}
export class ArtifactLike {
    constructor(init) { Object.assign(this, init); }
    id;
    artifactId;
    appUserId;
    createdDate;
}
export var ReportType;
(function (ReportType) {
    ReportType["Nsfw"] = "Nsfw";
    ReportType["Malformed"] = "Malformed";
    ReportType["Blurred"] = "Blurred";
    ReportType["LowQuality"] = "LowQuality";
    ReportType["Other"] = "Other";
})(ReportType || (ReportType = {}));
export class ArtifactReport {
    constructor(init) { Object.assign(this, init); }
    id;
    artifactId;
    appUserId;
    artifact;
    type;
    description;
    createdDate;
    notes;
    actionedDate;
    actionedBy;
}
export class StatBase {
    constructor(init) { Object.assign(this, init); }
    refId;
    appUserId;
    rawUrl;
    remoteIp;
    createdDate;
}
export class SearchStat extends StatBase {
    constructor(init) { super(init); Object.assign(this, init); }
    id;
    query;
    similar;
    user;
    modifier;
    artist;
    album;
    show;
    source;
    artifactId;
    albumId;
    modifierId;
    artistId;
}
export var StatType;
(function (StatType) {
    StatType["Download"] = "Download";
})(StatType || (StatType = {}));
export class ArtifactStat extends StatBase {
    constructor(init) { super(init); Object.assign(this, init); }
    id;
    type;
    artifactId;
    source;
    version;
}
export class ArtifactCommentVote {
    constructor(init) { Object.assign(this, init); }
    id;
    artifactCommentId;
    appUserId;
    vote;
    createdDate;
}
export class Artist extends AuditBase {
    constructor(init) { super(init); Object.assign(this, init); }
    id;
    firstName;
    lastName;
    yearDied;
    type;
    score;
    rank;
}
export class CreativeArtist {
    constructor(init) { Object.assign(this, init); }
    id;
    creativeId;
    artistId;
    artist;
}
export class Modifier extends AuditBase {
    constructor(init) { super(init); Object.assign(this, init); }
    id;
    name;
    category;
    description;
    score;
    rank;
}
export class CreativeModifier {
    constructor(init) { Object.assign(this, init); }
    id;
    creativeId;
    modifierId;
    modifier;
}
export class Creative extends AuditBase {
    constructor(init) { super(init); Object.assign(this, init); }
    id;
    userPrompt;
    prompt;
    images;
    width;
    height;
    steps;
    curatedArtifactId;
    primaryArtifactId;
    artistNames;
    modifierNames;
    artists;
    modifiers;
    artifacts;
    error;
    ownerId;
    ownerRef;
    key;
    curated;
    rating;
    private;
    score;
    rank;
    refId;
    requestId;
    engineId;
}
export var SignupType;
(function (SignupType) {
    SignupType["Updates"] = "Updates";
    SignupType["Beta"] = "Beta";
})(SignupType || (SignupType = {}));
export class Signup extends StatBase {
    constructor(init) { super(init); Object.assign(this, init); }
    id;
    type;
    email;
    name;
    cancelledDate;
}
export class AppUser {
    constructor(init) { Object.assign(this, init); }
    id;
    userName;
    displayName;
    firstName;
    lastName;
    handle;
    company;
    email;
    profileUrl;
    avatar;
    lastLoginIp;
    isArchived;
    archivedDate;
    lastLoginDate;
    phoneNumber;
    birthDate;
    birthDateRaw;
    address;
    address2;
    city;
    state;
    country;
    culture;
    fullName;
    gender;
    language;
    mailAddress;
    nickname;
    postalCode;
    timeZone;
    meta;
    primaryEmail;
    roles;
    permissions;
    refId;
    refIdStr;
    invalidLoginAttempts;
    lastLoginAttempt;
    lockedDate;
    createdDate;
    modifiedDate;
}
export class QueryBase {
    constructor(init) { Object.assign(this, init); }
    skip;
    take;
    orderBy;
    orderByDesc;
    include;
    fields;
    meta;
}
export class QueryDb_2 extends QueryBase {
    constructor(init) { super(init); Object.assign(this, init); }
}
export class ArtifactResult extends Artifact {
    constructor(init) { super(init); Object.assign(this, init); }
    userPrompt;
    artistNames;
    modifierNames;
    primaryArtifactId;
    ownerRef;
    similarity;
}
export class QueryData extends QueryBase {
    constructor(init) { super(init); Object.assign(this, init); }
}
export class QueryDb_1 extends QueryBase {
    constructor(init) { super(init); Object.assign(this, init); }
}
export class CommentResult {
    constructor(init) { Object.assign(this, init); }
    id;
    artifactId;
    replyId;
    content;
    upVotes;
    downVotes;
    votes;
    flagReason;
    notes;
    appUserId;
    displayName;
    handle;
    profileUrl;
    avatar;
    createdDate;
    modifiedDate;
}
export class ArtifactComment extends AuditBase {
    constructor(init) { super(init); Object.assign(this, init); }
    id;
    artifactId;
    replyId;
    content;
    upVotes;
    downVotes;
    votes;
    flagReason;
    notes;
    refId;
    appUserId;
}
export var PostReport;
(function (PostReport) {
    PostReport["Offensive"] = "Offensive";
    PostReport["Spam"] = "Spam";
    PostReport["Nudity"] = "Nudity";
    PostReport["Illegal"] = "Illegal";
})(PostReport || (PostReport = {}));
export class ArtifactCommentReport {
    constructor(init) { Object.assign(this, init); }
    id;
    artifactCommentId;
    appUserId;
    postReport;
    description;
    createdDate;
}
export class AlbumResult {
    constructor(init) { Object.assign(this, init); }
    id;
    name;
    slug;
    albumRef;
    ownerRef;
    primaryArtifactId;
    score;
    artifactIds;
}
export class ResponseError {
    constructor(init) { Object.assign(this, init); }
    errorCode;
    fieldName;
    message;
    meta;
}
export class ResponseStatus {
    constructor(init) { Object.assign(this, init); }
    errorCode;
    message;
    stackTrace;
    errors;
    meta;
}
export class ArtistInfo {
    constructor(init) { Object.assign(this, init); }
    id;
    name;
    type;
}
export class ModifierInfo {
    constructor(init) { Object.assign(this, init); }
    id;
    name;
    category;
}
export class Likes {
    constructor(init) { Object.assign(this, init); }
    artifactIds;
    albumIds;
}
export class UserResult {
    constructor(init) { Object.assign(this, init); }
    refId;
    handle;
    avatar;
    profileUrl;
    likes;
    albums;
}
export class AlbumRef {
    constructor(init) { Object.assign(this, init); }
    refId;
    ownerId;
    name;
    slug;
    description;
    tags;
    primaryArtifactRef;
}
export class GetCreativesInAlbumsResponse {
    constructor(init) { Object.assign(this, init); }
    results;
}
export class IdResponse {
    constructor(init) { Object.assign(this, init); }
    id;
    responseStatus;
}
export class GetArtifactUserDataResponse {
    constructor(init) { Object.assign(this, init); }
    artifactId;
    liked;
    upVoted;
    downVoted;
}
export class CheckQuotaResponse {
    constructor(init) { Object.assign(this, init); }
    timeRemaining;
    creditsUsed;
    creditsRequested;
    creditsRemaining;
    dailyQuota;
    requestedDetails;
}
export class CreateCreativeResponse {
    constructor(init) { Object.assign(this, init); }
    result;
    responseStatus;
}
export class GetCreativeResponse {
    constructor(init) { Object.assign(this, init); }
    result;
    responseStatus;
}
export class SearchDataResponse {
    constructor(init) { Object.assign(this, init); }
    artists;
    modifiers;
}
export class GetUserInfoResponse {
    constructor(init) { Object.assign(this, init); }
    result;
    responseStatus;
}
export class AnonDataResponse {
    constructor(init) { Object.assign(this, init); }
    topAlbums;
    responseStatus;
}
export class GetAlbumIdsResponse {
    constructor(init) { Object.assign(this, init); }
    results;
}
export class GetAlbumRefsResponse {
    constructor(init) { Object.assign(this, init); }
    results;
}
export class UserDataResponse {
    constructor(init) { Object.assign(this, init); }
    user;
    signups;
    roles;
    responseStatus;
}
export class GetAlbumResultsResponse {
    constructor(init) { Object.assign(this, init); }
    results;
}
export class EmptyResponse {
    constructor(init) { Object.assign(this, init); }
    responseStatus;
}
export class UserProfile {
    constructor(init) { Object.assign(this, init); }
    displayName;
    avatar;
    handle;
}
export class GetUserProfileResponse {
    constructor(init) { Object.assign(this, init); }
    result;
    responseStatus;
}
export class QueryResponse {
    constructor(init) { Object.assign(this, init); }
    offset;
    total;
    results;
    meta;
    responseStatus;
}
export class PrerenderResponse {
    constructor(init) { Object.assign(this, init); }
    results;
    responseStatus;
}
export class Todo {
    constructor(init) { Object.assign(this, init); }
    id;
    text;
    isFinished;
}
export class AuthenticateResponse {
    constructor(init) { Object.assign(this, init); }
    userId;
    sessionId;
    userName;
    displayName;
    referrerUrl;
    bearerToken;
    refreshToken;
    profileUrl;
    roles;
    permissions;
    responseStatus;
    meta;
}
export class AssignRolesResponse {
    constructor(init) { Object.assign(this, init); }
    allRoles;
    allPermissions;
    meta;
    responseStatus;
}
export class UnAssignRolesResponse {
    constructor(init) { Object.assign(this, init); }
    allRoles;
    allPermissions;
    meta;
    responseStatus;
}
export class RegisterResponse {
    constructor(init) { Object.assign(this, init); }
    userId;
    sessionId;
    userName;
    referrerUrl;
    bearerToken;
    refreshToken;
    roles;
    permissions;
    responseStatus;
    meta;
}
export class CreateAlbum {
    constructor(init) { Object.assign(this, init); }
    name;
    description;
    tags;
    primaryArtifactId;
    artifactIds;
    getTypeName() { return 'CreateAlbum'; };
    getMethod() { return 'POST'; };
    createResponse() { return new Album(); };
}
export class UpdateAlbum {
    constructor(init) { Object.assign(this, init); }
    id;
    name;
    description;
    slug;
    tags;
    primaryArtifactId;
    unpinPrimaryArtifact;
    addArtifactIds;
    removeArtifactIds;
    getTypeName() { return 'UpdateAlbum'; };
    getMethod() { return 'PATCH'; };
    createResponse() { return new Album(); };
}
export class GetCreativesInAlbums {
    constructor(init) { Object.assign(this, init); }
    creativeId;
    getTypeName() { return 'GetCreativesInAlbums'; };
    getMethod() { return 'GET'; };
    createResponse() { return new GetCreativesInAlbumsResponse(); };
}
export class CreateAlbumLike {
    constructor(init) { Object.assign(this, init); }
    albumId;
    getTypeName() { return 'CreateAlbumLike'; };
    getMethod() { return 'POST'; };
    createResponse() { return new IdResponse(); };
}
export class DeleteAlbumLike {
    constructor(init) { Object.assign(this, init); }
    albumId;
    getTypeName() { return 'DeleteAlbumLike'; };
    getMethod() { return 'DELETE'; };
    createResponse() { };
}
export class CreateArtifactLike {
    constructor(init) { Object.assign(this, init); }
    artifactId;
    getTypeName() { return 'CreateArtifactLike'; };
    getMethod() { return 'POST'; };
    createResponse() { return new IdResponse(); };
}
export class DeleteArtifactLike {
    constructor(init) { Object.assign(this, init); }
    artifactId;
    getTypeName() { return 'DeleteArtifactLike'; };
    getMethod() { return 'DELETE'; };
    createResponse() { };
}
export class CreateArtifactReport {
    constructor(init) { Object.assign(this, init); }
    artifactId;
    type;
    description;
    getTypeName() { return 'CreateArtifactReport'; };
    getMethod() { return 'POST'; };
    createResponse() { return new ArtifactReport(); };
}
export class DeleteArtifactReport {
    constructor(init) { Object.assign(this, init); }
    artifactId;
    getTypeName() { return 'DeleteArtifactReport'; };
    getMethod() { return 'DELETE'; };
    createResponse() { };
}
export class DownloadArtifact {
    constructor(init) { Object.assign(this, init); }
    refId;
    getTypeName() { return 'DownloadArtifact'; };
    getMethod() { return 'GET'; };
    createResponse() { return new Blob(); };
}
export class DownloadDirect {
    constructor(init) { Object.assign(this, init); }
    refId;
    encryptionMethod;
    accessId;
    accessKey;
    getTypeName() { return 'DownloadDirect'; };
    getMethod() { return 'POST'; };
    createResponse () { };
}
export class AnalyticsTasks {
    constructor(init) { Object.assign(this, init); }
    recordSearchStat;
    recordArtifactStat;
    getTypeName() { return 'AnalyticsTasks'; };
    getMethod() { return 'POST'; };
    createResponse () { };
}
export class GetArtifactUserData {
    constructor(init) { Object.assign(this, init); }
    artifactId;
    getTypeName() { return 'GetArtifactUserData'; };
    getMethod() { return 'GET'; };
    createResponse() { return new GetArtifactUserDataResponse(); };
}
export class CreateArtifactCommentVote {
    constructor(init) { Object.assign(this, init); }
    artifactCommentId;
    vote;
    getTypeName() { return 'CreateArtifactCommentVote'; };
    getMethod() { return 'POST'; };
    createResponse() { };
}
export class DeleteArtifactCommentVote {
    constructor(init) { Object.assign(this, init); }
    artifactCommentId;
    getTypeName() { return 'DeleteArtifactCommentVote'; };
    getMethod() { return 'DELETE'; };
    createResponse() { };
}
export class CheckQuota {
    constructor(init) { Object.assign(this, init); }
    images;
    width;
    height;
    getTypeName() { return 'CheckQuota'; };
    getMethod() { return 'POST'; };
    createResponse() { return new CheckQuotaResponse(); };
}
export class CreateCreative {
    constructor(init) { Object.assign(this, init); }
    userPrompt;
    images;
    width;
    height;
    steps;
    seed;
    artistIds;
    modifierIds;
    getTypeName() { return 'CreateCreative'; };
    getMethod() { return 'POST'; };
    createResponse() { return new CreateCreativeResponse(); };
}
export class UpdateCreative {
    constructor(init) { Object.assign(this, init); }
    id;
    primaryArtifactId;
    unpinPrimaryArtifact;
    getTypeName() { return 'UpdateCreative'; };
    getMethod() { return 'PATCH'; };
    createResponse() { return new Creative(); };
}
export class UpdateArtifact {
    constructor(init) { Object.assign(this, init); }
    id;
    nsfw;
    quality;
    getTypeName() { return 'UpdateArtifact'; };
    getMethod() { return 'PATCH'; };
    createResponse() { return new Artifact(); };
}
export class DeleteCreative {
    constructor(init) { Object.assign(this, init); }
    id;
    getTypeName() { return 'DeleteCreative'; };
    getMethod() { return 'DELETE'; };
    createResponse() { };
}
export class HardDeleteCreative {
    constructor(init) { Object.assign(this, init); }
    id;
    getTypeName() { return 'HardDeleteCreative'; };
    getMethod() { return 'DELETE'; };
    createResponse() { };
}
export class DeleteArtifactHtml {
    constructor(init) { Object.assign(this, init); }
    ids;
    getTypeName() { return 'DeleteArtifactHtml'; };
    getMethod() { return 'POST'; };
    createResponse () { };
}
export class DeleteCdnFilesMq {
    constructor(init) { Object.assign(this, init); }
    files;
    getTypeName() { return 'DeleteCdnFilesMq'; };
    getMethod() { return 'POST'; };
    createResponse () { };
}
export class DeleteCdnFile {
    constructor(init) { Object.assign(this, init); }
    file;
    getTypeName() { return 'DeleteCdnFile'; };
    getMethod() { return 'POST'; };
    createResponse() { };
}
export class GetCdnFile {
    constructor(init) { Object.assign(this, init); }
    file;
    getTypeName() { return 'GetCdnFile'; };
    getMethod() { return 'POST'; };
    createResponse () { };
}
export class GetCreative {
    constructor(init) { Object.assign(this, init); }
    id;
    artifactId;
    getTypeName() { return 'GetCreative'; };
    getMethod() { return 'GET'; };
    createResponse() { return new GetCreativeResponse(); };
}
export class SearchData {
    constructor(init) { Object.assign(this, init); }
    getTypeName() { return 'SearchData'; };
    getMethod() { return 'POST'; };
    createResponse() { return new SearchDataResponse(); };
}
export class GetUserInfo {
    constructor(init) { Object.assign(this, init); }
    refId;
    getTypeName() { return 'GetUserInfo'; };
    getMethod() { return 'POST'; };
    createResponse() { return new GetUserInfoResponse(); };
}
export class AnonData {
    constructor(init) { Object.assign(this, init); }
    getTypeName() { return 'AnonData'; };
    getMethod() { return 'POST'; };
    createResponse() { return new AnonDataResponse(); };
}
export class GetAlbumIds {
    constructor(init) { Object.assign(this, init); }
    getTypeName() { return 'GetAlbumIds'; };
    getMethod() { return 'POST'; };
    createResponse() { return new GetAlbumIdsResponse(); };
}
export class GetAlbumRefs {
    constructor(init) { Object.assign(this, init); }
    getTypeName() { return 'GetAlbumRefs'; };
    getMethod() { return 'POST'; };
    createResponse() { return new GetAlbumRefsResponse(); };
}
export class UserData {
    constructor(init) { Object.assign(this, init); }
    getTypeName() { return 'UserData'; };
    getMethod() { return 'POST'; };
    createResponse() { return new UserDataResponse(); };
}
export class GetAlbumResults {
    constructor(init) { Object.assign(this, init); }
    ids;
    refIds;
    getTypeName() { return 'GetAlbumResults'; };
    getMethod() { return 'POST'; };
    createResponse() { return new GetAlbumResultsResponse(); };
}
export class CreateSignup {
    constructor(init) { Object.assign(this, init); }
    type;
    email;
    name;
    getTypeName() { return 'CreateSignup'; };
    getMethod() { return 'POST'; };
    createResponse() { return new EmptyResponse(); };
}
export class GetUserProfile {
    constructor(init) { Object.assign(this, init); }
    getTypeName() { return 'GetUserProfile'; };
    getMethod() { return 'POST'; };
    createResponse() { return new GetUserProfileResponse(); };
}
export class UpdateUserProfile {
    constructor(init) { Object.assign(this, init); }
    displayName;
    avatar;
    handle;
    getTypeName() { return 'UpdateUserProfile'; };
    getMethod() { return 'PUT'; };
    createResponse() { return new UserProfile(); };
}
export class SearchArtifacts extends QueryDb_2 {
    constructor(init) { super(init); Object.assign(this, init); }
    query;
    similar;
    by;
    user;
    show;
    modifier;
    artist;
    album;
    source;
    getTypeName() { return 'SearchArtifacts'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class ViewCreativeMetadata {
    constructor(init) { Object.assign(this, init); }
    creativeId;
    getTypeName() { return 'ViewCreativeMetadata'; };
    getMethod() { return 'GET'; };
    createResponse() { return new Creative(); };
}
export class TestImageHtml {
    constructor(init) { Object.assign(this, init); }
    getTypeName() { return 'TestImageHtml'; };
    getMethod() { return 'POST'; };
    createResponse() { };
}
export class PrerenderImages {
    constructor(init) { Object.assign(this, init); }
    batches;
    getTypeName() { return 'PrerenderImages'; };
    getMethod() { return 'POST'; };
    createResponse() { return new PrerenderResponse(); };
}
export class RenderArtifactHtml {
    constructor(init) { Object.assign(this, init); }
    group;
    id;
    slug;
    getTypeName() { return 'RenderArtifactHtml'; };
    getMethod() { return 'POST'; };
    createResponse() { return ''; };
}
export class QueryTodos extends QueryData {
    constructor(init) { super(init); Object.assign(this, init); }
    id;
    ids;
    textContains;
    getTypeName() { return 'QueryTodos'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class CreateTodo {
    constructor(init) { Object.assign(this, init); }
    text;
    getTypeName() { return 'CreateTodo'; };
    getMethod() { return 'POST'; };
    createResponse() { return new Todo(); };
}
export class UpdateTodo {
    constructor(init) { Object.assign(this, init); }
    id;
    text;
    isFinished;
    getTypeName() { return 'UpdateTodo'; };
    getMethod() { return 'PUT'; };
    createResponse() { return new Todo(); };
}
export class DeleteTodo {
    constructor(init) { Object.assign(this, init); }
    id;
    getTypeName() { return 'DeleteTodo'; };
    getMethod() { return 'DELETE'; };
    createResponse() { };
}
export class DeleteTodos {
    constructor(init) { Object.assign(this, init); }
    ids;
    getTypeName() { return 'DeleteTodos'; };
    getMethod() { return 'DELETE'; };
    createResponse() { };
}
export class Authenticate {
    constructor(init) { Object.assign(this, init); }
    provider;
    state;
    oauth_token;
    oauth_verifier;
    userName;
    password;
    rememberMe;
    errorView;
    nonce;
    uri;
    response;
    qop;
    nc;
    cnonce;
    accessToken;
    accessTokenSecret;
    scope;
    meta;
    getTypeName() { return 'Authenticate'; };
    getMethod() { return 'POST'; };
    createResponse() { return new AuthenticateResponse(); };
}
export class AssignRoles {
    constructor(init) { Object.assign(this, init); }
    userName;
    permissions;
    roles;
    meta;
    getTypeName() { return 'AssignRoles'; };
    getMethod() { return 'POST'; };
    createResponse() { return new AssignRolesResponse(); };
}
export class UnAssignRoles {
    constructor(init) { Object.assign(this, init); }
    userName;
    permissions;
    roles;
    meta;
    getTypeName() { return 'UnAssignRoles'; };
    getMethod() { return 'POST'; };
    createResponse() { return new UnAssignRolesResponse(); };
}
export class Register {
    constructor(init) { Object.assign(this, init); }
    userName;
    firstName;
    lastName;
    displayName;
    email;
    password;
    confirmPassword;
    autoLogin;
    errorView;
    meta;
    getTypeName() { return 'Register'; };
    getMethod() { return 'POST'; };
    createResponse() { return new RegisterResponse(); };
}
export class QueryAlbums extends QueryDb_1 {
    constructor(init) { super(init); Object.assign(this, init); }
    id;
    ids;
    getTypeName() { return 'QueryAlbums'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QueryAlbumLikes extends QueryDb_1 {
    constructor(init) { super(init); Object.assign(this, init); }
    getTypeName() { return 'QueryAlbumLikes'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QueryAlbumArtifacts extends QueryDb_1 {
    constructor(init) { super(init); Object.assign(this, init); }
    getTypeName() { return 'QueryAlbumArtifacts'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QueryArtifactStats extends QueryDb_1 {
    constructor(init) { super(init); Object.assign(this, init); }
    getTypeName() { return 'QueryArtifactStats'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QuerySearchStats extends QueryDb_1 {
    constructor(init) { super(init); Object.assign(this, init); }
    getTypeName() { return 'QuerySearchStats'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QuerySignups extends QueryDb_1 {
    constructor(init) { super(init); Object.assign(this, init); }
    getTypeName() { return 'QuerySignups'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QueryArtifacts extends QueryDb_1 {
    constructor(init) { super(init); Object.assign(this, init); }
    id;
    ids;
    getTypeName() { return 'QueryArtifacts'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QueryArtifactLikes extends QueryDb_1 {
    constructor(init) { super(init); Object.assign(this, init); }
    getTypeName() { return 'QueryArtifactLikes'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QueryArtifactReports extends QueryDb_1 {
    constructor(init) { super(init); Object.assign(this, init); }
    artifactId;
    getTypeName() { return 'QueryArtifactReports'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QueryArtifactComments extends QueryDb_2 {
    constructor(init) { super(init); Object.assign(this, init); }
    artifactId;
    getTypeName() { return 'QueryArtifactComments'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QueryArtifactCommentVotes extends QueryDb_1 {
    constructor(init) { super(init); Object.assign(this, init); }
    artifactId;
    getTypeName() { return 'QueryArtifactCommentVotes'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QueryCreatives extends QueryDb_1 {
    constructor(init) { super(init); Object.assign(this, init); }
    id;
    createdBy;
    ownerId;
    getTypeName() { return 'QueryCreatives'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QueryArtists extends QueryDb_1 {
    constructor(init) { super(init); Object.assign(this, init); }
    getTypeName() { return 'QueryArtists'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QueryModifiers extends QueryDb_1 {
    constructor(init) { super(init); Object.assign(this, init); }
    getTypeName() { return 'QueryModifiers'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QueryCreativeArtists extends QueryDb_1 {
    constructor(init) { super(init); Object.assign(this, init); }
    creativeId;
    modifierId;
    getTypeName() { return 'QueryCreativeArtists'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QueryCreativeModifiers extends QueryDb_1 {
    constructor(init) { super(init); Object.assign(this, init); }
    creativeId;
    modifierId;
    getTypeName() { return 'QueryCreativeModifiers'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class DeleteAlbum {
    constructor(init) { Object.assign(this, init); }
    id;
    getTypeName() { return 'DeleteAlbum'; };
    getMethod() { return 'DELETE'; };
    createResponse() { };
}
export class UpdateAlbumArtifact {
    constructor(init) { Object.assign(this, init); }
    id;
    name;
    description;
    slug;
    tags;
    primaryArtifactId;
    addArtifactIds;
    removeArtifactIds;
    getTypeName() { return 'UpdateAlbumArtifact'; };
    getMethod() { return 'PATCH'; };
    createResponse() { return new Album(); };
}
export class UpdateSignup {
    constructor(init) { Object.assign(this, init); }
    id;
    type;
    email;
    name;
    cancelledDate;
    getTypeName() { return 'UpdateSignup'; };
    getMethod() { return 'PATCH'; };
    createResponse() { return new Signup(); };
}
export class DeleteSignup {
    constructor(init) { Object.assign(this, init); }
    id;
    getTypeName() { return 'DeleteSignup'; };
    getMethod() { return 'DELETE'; };
    createResponse() { };
}
export class UpdateArtifactReport {
    constructor(init) { Object.assign(this, init); }
    artifactId;
    type;
    description;
    getTypeName() { return 'UpdateArtifactReport'; };
    getMethod() { return 'PATCH'; };
    createResponse() { return new ArtifactReport(); };
}
export class CreateArtifactComment {
    constructor(init) { Object.assign(this, init); }
    artifactId;
    replyId;
    content;
    getTypeName() { return 'CreateArtifactComment'; };
    getMethod() { return 'POST'; };
    createResponse() { return new ArtifactComment(); };
}
export class UpdateArtifactComment {
    constructor(init) { Object.assign(this, init); }
    id;
    content;
    getTypeName() { return 'UpdateArtifactComment'; };
    getMethod() { return 'PATCH'; };
    createResponse() { return new ArtifactComment(); };
}
export class DeleteArtifactComment {
    constructor(init) { Object.assign(this, init); }
    id;
    getTypeName() { return 'DeleteArtifactComment'; };
    getMethod() { return 'DELETE'; };
    createResponse() { };
}
export class CreateArtifactCommentReport {
    constructor(init) { Object.assign(this, init); }
    artifactCommentId;
    postReport;
    description;
    getTypeName() { return 'CreateArtifactCommentReport'; };
    getMethod() { return 'POST'; };
    createResponse() { };
}
export class DeleteArtifactCommentReport {
    constructor(init) { Object.assign(this, init); }
    id;
    getTypeName() { return 'DeleteArtifactCommentReport'; };
    getMethod() { return 'DELETE'; };
    createResponse() { };
}
export class CreateArtist {
    constructor(init) { Object.assign(this, init); }
    firstName;
    lastName;
    yearDied;
    type;
    getTypeName() { return 'CreateArtist'; };
    getMethod() { return 'POST'; };
    createResponse() { return new Artist(); };
}
export class UpdateArtist {
    constructor(init) { Object.assign(this, init); }
    id;
    firstName;
    lastName;
    yearDied;
    type;
    getTypeName() { return 'UpdateArtist'; };
    getMethod() { return 'PATCH'; };
    createResponse() { return new Artist(); };
}
export class DeleteArtist {
    constructor(init) { Object.assign(this, init); }
    id;
    getTypeName() { return 'DeleteArtist'; };
    getMethod() { return 'DELETE'; };
    createResponse() { };
}
export class CreateModifier {
    constructor(init) { Object.assign(this, init); }
    name;
    category;
    description;
    getTypeName() { return 'CreateModifier'; };
    getMethod() { return 'POST'; };
    createResponse() { return new Modifier(); };
}
export class UpdateModifier {
    constructor(init) { Object.assign(this, init); }
    id;
    name;
    category;
    description;
    getTypeName() { return 'UpdateModifier'; };
    getMethod() { return 'PATCH'; };
    createResponse() { return new Modifier(); };
}
export class DeleteModifier {
    constructor(init) { Object.assign(this, init); }
    id;
    getTypeName() { return 'DeleteModifier'; };
    getMethod() { return 'DELETE'; };
    createResponse() { };
}
export class CreateCreativeArtist {
    constructor(init) { Object.assign(this, init); }
    creativeId;
    modifierId;
    getTypeName() { return 'CreateCreativeArtist'; };
    getMethod() { return 'POST'; };
    createResponse() { return new CreativeArtist(); };
}
export class DeleteCreativeArtist {
    constructor(init) { Object.assign(this, init); }
    id;
    ids;
    getTypeName() { return 'DeleteCreativeArtist'; };
    getMethod() { return 'DELETE'; };
    createResponse() { };
}
export class CreateCreativeModifier {
    constructor(init) { Object.assign(this, init); }
    creativeId;
    modifierId;
    getTypeName() { return 'CreateCreativeModifier'; };
    getMethod() { return 'POST'; };
    createResponse() { return new CreativeModifier(); };
}
export class DeleteCreativeModifier {
    constructor(init) { Object.assign(this, init); }
    id;
    ids;
    getTypeName() { return 'DeleteCreativeModifier'; };
    getMethod() { return 'DELETE'; };
    createResponse() { };
}

