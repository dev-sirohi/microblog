import React from 'react';
import type {
    GetPostRequest,
    Post
} from "../interfaces/GlobalInterfaceExport";
import PostApi from "../api/PostApi";
import { GlobalDialog } from '../globalDialogRef';

export default function Home(): React.ReactNode {
    const [posts, setPosts] = React.useState<Post[]>([]);
    const [pageData, setPageData] = React.useState<any>({});
    const [loading, setLoading] = React.useState<boolean>(false);
    const [pageNumber, setPageNumber] = React.useState<number>(1);
    const [pageSize, setPageSize] = React.useState<number>(10);
    const [hasMore, setHasMore] = React.useState<boolean>(true);

    const loaderRef = React.useRef<HTMLDivElement | null>(null);

    const loadPosts = async () => {
        if (loading) return;
        setLoading(true);
        try {
            const getPostReqObj: GetPostRequest = {
                page: pageNumber,
                pageSize: pageSize,
            };
            const res = await PostApi.getHomeFeed(getPostReqObj);
            if (!res.Success) {
                throw new Error(res.Message || "Unable to fetch posts");
            }
            const newPosts: Post[] = res.Data ?? [];
            if (newPosts.length === 0) {
                setHasMore(false);
            }
            // Using functional method because writing [...posts, ...newPosts] risks using stale data
            // post => ... always fetches the latest data
            setPosts(post => [...post, ...newPosts]);
        } catch (ex: any) {
            GlobalDialog.showError(ex.message);
            setHasMore(false);
        } finally {
            setLoading(false);
        }
    }

    React.useEffect(() => {
        loadPosts();
    }, [pageNumber]);

    React.useEffect(() => {
        if ((posts || []).length === 0) return;
        if (!loaderRef.current || !hasMore) return;
        const observer = new IntersectionObserver((entries) => {
            const loader = entries[0];
            if (loader.isIntersecting && !loading) {
                setPageNumber(prev => prev + 1);
            }
        }, {
            root: null,
            rootMargin: "0px",
            threshold: 1.0,
        });
        observer.observe(loaderRef.current);
        // Clean up so that on useEffect reload we don't have multiple current and past observers sending multiple API calls
        return () => observer.disconnect();
    }, [loading]);

    return (
        <div style={{ width: "600px", margin: "0 auto" }}>
            {posts.map((p, i) => (
                <div
                    key={p.id}
                    style={{
                        border: "1px solid #e0e0e0",
                        padding: "16px",
                        marginBottom: "16px",
                        borderRadius: "10px",
                        background: "#fff",
                        boxShadow: "0 1px 3px rgba(0,0,0,0.06)"
                    }}
                >
                    <div style={{
                        display: "flex",
                        justifyContent: "space-between",
                        marginBottom: "8px"
                    }}>
                        <span style={{ fontWeight: "bold" }}>User {p.userId}</span>
                        <span style={{ color: "#777", fontSize: "0.85rem" }}>
                            {new Date(p.createdAt).toLocaleString()}
                        </span>
                    </div>
                    <p style={{ fontSize: "1rem", marginBottom: "10px" }}>
                        {p.content}
                    </p>
                    {p.medialUrl && (
                        <img
                            src={p.medialUrl}
                            alt="post"
                            style={{
                                width: "100%",
                                maxHeight: "350px",
                                objectFit: "cover",
                                borderRadius: "8px",
                                marginBottom: "10px"
                            }}
                        />
                    )}
                    <div>
                        {p.tags?.map((t, tagIndex) => (
                            <span
                                key={tagIndex}
                                style={{
                                    display: "inline-block",
                                    padding: "4px 8px",
                                    background: "#f2f2f2",
                                    borderRadius: "6px",
                                    fontSize: "0.85rem",
                                    marginRight: "6px",
                                    marginBottom: "6px"
                                }}
                            >
                                #{t}
                            </span>
                        ))}
                    </div>
                </div>
            ))}

            <div ref={loaderRef} style={{ height: "40px" }} />

            {loading && <p>Loading...</p>}
        </div>
    );
}